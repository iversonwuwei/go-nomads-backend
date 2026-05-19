package cache

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"math"
	"net/http"
	"strings"
	"time"
)

type Upstream interface {
	GetCityScore(ctx context.Context, cityID string) (ScoreResponse, error)
	GetCityScoresBatch(ctx context.Context, cityIDs []string) ([]ScoreResponse, error)
	GetCityCost(ctx context.Context, cityID string) (CostResponse, error)
	GetCityCostsBatch(ctx context.Context, cityIDs []string) ([]CostResponse, error)
	GetCoworkingScore(ctx context.Context, coworkingID string) (ScoreResponse, error)
	GetCoworkingScoresBatch(ctx context.Context, coworkingIDs []string) ([]ScoreResponse, error)
}

type HTTPUpstream struct {
	client              *http.Client
	logger              *slog.Logger
	cityServiceURL      string
	coworkingServiceURL string
}

func NewHTTPUpstream(config Config, logger *slog.Logger) *HTTPUpstream {
	if logger == nil {
		logger = slog.Default()
	}
	return &HTTPUpstream{
		client:              &http.Client{Timeout: config.RequestTimeout},
		logger:              logger,
		cityServiceURL:      strings.TrimRight(config.CityServiceURL, "/"),
		coworkingServiceURL: strings.TrimRight(config.CoworkingServiceURL, "/"),
	}
}

func (upstream *HTTPUpstream) GetCityScore(ctx context.Context, cityID string) (ScoreResponse, error) {
	var response cityRatingStatsResponse
	if err := upstream.doJSON(ctx, http.MethodGet, upstream.cityServiceURL, "/api/v1/cities/"+cityID+"/ratings/statistics", nil, &response); err != nil {
		upstream.logger.Warn("city score fallback to zero after upstream failure", "cityId", cityID, "error", err)
		return ScoreResponse{EntityID: cityID, OverallScore: 0, FromCache: false}, nil
	}
	statistics, overallScore := cityScoreStatistics(response.Statistics)
	return ScoreResponse{EntityID: cityID, OverallScore: overallScore, FromCache: false, Statistics: statistics}, nil
}

func (upstream *HTTPUpstream) GetCityScoresBatch(ctx context.Context, cityIDs []string) ([]ScoreResponse, error) {
	if len(cityIDs) == 0 {
		return nil, nil
	}
	var response batchCityRatingStatsResponse
	if err := upstream.doJSON(ctx, http.MethodPost, upstream.cityServiceURL, "/api/v1/cities/ratings/statistics/batch", cityIDs, &response); err == nil {
		result := make([]ScoreResponse, 0, len(cityIDs))
		for _, cityID := range cityIDs {
			stats := response.CityStatistics[cityID]
			statistics, overallScore := cityScoreStatistics(stats.Statistics)
			result = append(result, ScoreResponse{EntityID: cityID, OverallScore: overallScore, FromCache: false, Statistics: statistics})
		}
		return result, nil
	}

	result := make([]ScoreResponse, 0, len(cityIDs))
	for _, cityID := range cityIDs {
		score, _ := upstream.GetCityScore(ctx, cityID)
		result = append(result, score)
	}
	return result, nil
}

func (upstream *HTTPUpstream) GetCityCost(ctx context.Context, cityID string) (CostResponse, error) {
	var response cityCostStatsResponse
	if err := upstream.doJSON(ctx, http.MethodGet, upstream.cityServiceURL, "/api/v1/cities/"+cityID+"/expenses/statistics", nil, &response); err != nil {
		upstream.logger.Warn("city cost fallback to zero after upstream failure", "cityId", cityID, "error", err)
		return CostResponse{EntityID: cityID, AverageCost: 0, FromCache: false}, nil
	}
	if response.TotalAverageCost == 0 {
		return CostResponse{EntityID: cityID, AverageCost: 0, FromCache: false}, nil
	}
	statistics := doubleSerializeString(string(mustJSON(cityCostStatsResponse{
		TotalAverageCost:  response.TotalAverageCost,
		CategoryCosts:     response.CategoryCosts,
		ContributorCount:  response.ContributorCount,
		TotalExpenseCount: response.TotalExpenseCount,
		Currency:          response.Currency,
		UpdatedAt:         response.UpdatedAt,
	})))
	return CostResponse{EntityID: cityID, AverageCost: response.TotalAverageCost, FromCache: false, Statistics: &statistics}, nil
}

func (upstream *HTTPUpstream) GetCityCostsBatch(ctx context.Context, cityIDs []string) ([]CostResponse, error) {
	result := make([]CostResponse, 0, len(cityIDs))
	for _, cityID := range cityIDs {
		cost, _ := upstream.GetCityCost(ctx, cityID)
		result = append(result, cost)
	}
	return result, nil
}

func (upstream *HTTPUpstream) GetCoworkingScore(ctx context.Context, coworkingID string) (ScoreResponse, error) {
	var response coworkingDetailResponse
	if err := upstream.doJSON(ctx, http.MethodGet, upstream.coworkingServiceURL, "/api/coworkings/"+coworkingID, nil, &response); err != nil {
		return ScoreResponse{}, err
	}
	statistics := string(mustJSON(struct {
		ReviewCount int `json:"ReviewCount"`
	}{ReviewCount: response.ReviewCount}))
	return ScoreResponse{EntityID: coworkingID, OverallScore: response.Rating, FromCache: false, Statistics: &statistics}, nil
}

func (upstream *HTTPUpstream) GetCoworkingScoresBatch(ctx context.Context, coworkingIDs []string) ([]ScoreResponse, error) {
	result := make([]ScoreResponse, 0, len(coworkingIDs))
	for _, coworkingID := range coworkingIDs {
		score, err := upstream.GetCoworkingScore(ctx, coworkingID)
		if err != nil {
			statistics := string(mustJSON(struct {
				ReviewCount int `json:"ReviewCount"`
			}{ReviewCount: 0}))
			result = append(result, ScoreResponse{EntityID: coworkingID, OverallScore: 0, FromCache: false, Statistics: &statistics})
			continue
		}
		result = append(result, score)
	}
	return result, nil
}

func (upstream *HTTPUpstream) doJSON(ctx context.Context, method string, baseURL string, path string, requestBody any, responseBody any) error {
	if baseURL == "" {
		return fmt.Errorf("missing upstream base url for %s %s", method, path)
	}
	var bodyReader *bytes.Reader
	if requestBody != nil {
		payload, err := json.Marshal(requestBody)
		if err != nil {
			return err
		}
		bodyReader = bytes.NewReader(payload)
	} else {
		bodyReader = bytes.NewReader(nil)
	}
	request, err := http.NewRequestWithContext(ctx, method, baseURL+path, bodyReader)
	if err != nil {
		return err
	}
	if requestBody != nil {
		request.Header.Set("Content-Type", "application/json")
	}
	response, err := upstream.client.Do(request)
	if err != nil {
		return err
	}
	defer response.Body.Close()
	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return fmt.Errorf("unexpected upstream status %d", response.StatusCode)
	}
	if responseBody == nil {
		return nil
	}
	return json.NewDecoder(response.Body).Decode(responseBody)
}

func cityScoreStatistics(statistics []categoryStatistic) (*string, float64) {
	if len(statistics) == 0 {
		return nil, 0
	}
	ratingsWithData := make([]categoryStatistic, 0, len(statistics))
	for _, statistic := range statistics {
		if statistic.RatingCount > 0 {
			ratingsWithData = append(ratingsWithData, statistic)
		}
	}
	if len(ratingsWithData) == 0 {
		return nil, 0
	}
	total := 0.0
	for _, statistic := range ratingsWithData {
		total += statistic.AverageRating
	}
	overallScore := total / float64(len(ratingsWithData))
	overallScore = math.Round(overallScore*100) / 100
	serialized := string(mustJSON(statistics))
	return &serialized, overallScore
}

func doubleSerializeString(value string) string {
	return string(mustJSON(value))
}

func mustJSON(value any) []byte {
	payload, err := json.Marshal(value)
	if err != nil {
		panic(err)
	}
	return payload
}

type StubUpstream struct {
	CityScores      map[string]ScoreResponse
	CityCosts       map[string]CostResponse
	CoworkingScores map[string]ScoreResponse
	ErrCoworkingIDs map[string]error
	ErrCityScoreIDs map[string]error
	ErrCityCostIDs  map[string]error
}

func (stub *StubUpstream) GetCityScore(_ context.Context, cityID string) (ScoreResponse, error) {
	if err := stub.ErrCityScoreIDs[cityID]; err != nil {
		return ScoreResponse{}, err
	}
	if response, ok := stub.CityScores[cityID]; ok {
		return response, nil
	}
	return ScoreResponse{EntityID: cityID}, nil
}

func (stub *StubUpstream) GetCityScoresBatch(ctx context.Context, cityIDs []string) ([]ScoreResponse, error) {
	result := make([]ScoreResponse, 0, len(cityIDs))
	for _, cityID := range cityIDs {
		response, err := stub.GetCityScore(ctx, cityID)
		if err != nil {
			response = ScoreResponse{EntityID: cityID}
		}
		result = append(result, response)
	}
	return result, nil
}

func (stub *StubUpstream) GetCityCost(_ context.Context, cityID string) (CostResponse, error) {
	if err := stub.ErrCityCostIDs[cityID]; err != nil {
		return CostResponse{}, err
	}
	if response, ok := stub.CityCosts[cityID]; ok {
		return response, nil
	}
	return CostResponse{EntityID: cityID}, nil
}

func (stub *StubUpstream) GetCityCostsBatch(ctx context.Context, cityIDs []string) ([]CostResponse, error) {
	result := make([]CostResponse, 0, len(cityIDs))
	for _, cityID := range cityIDs {
		response, err := stub.GetCityCost(ctx, cityID)
		if err != nil {
			response = CostResponse{EntityID: cityID}
		}
		result = append(result, response)
	}
	return result, nil
}

func (stub *StubUpstream) GetCoworkingScore(_ context.Context, coworkingID string) (ScoreResponse, error) {
	if err := stub.ErrCoworkingIDs[coworkingID]; err != nil {
		return ScoreResponse{}, err
	}
	if response, ok := stub.CoworkingScores[coworkingID]; ok {
		return response, nil
	}
	return ScoreResponse{EntityID: coworkingID}, nil
}

func (stub *StubUpstream) GetCoworkingScoresBatch(ctx context.Context, coworkingIDs []string) ([]ScoreResponse, error) {
	result := make([]ScoreResponse, 0, len(coworkingIDs))
	for _, coworkingID := range coworkingIDs {
		response, err := stub.GetCoworkingScore(ctx, coworkingID)
		if err != nil {
			response = ScoreResponse{EntityID: coworkingID}
		}
		result = append(result, response)
	}
	return result, nil
}

func ptr(value string) *string {
	return &value
}

func fixedTime(value string) time.Time {
	parsed, err := time.Parse(time.RFC3339, value)
	if err != nil {
		panic(err)
	}
	return parsed
}
