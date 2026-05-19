package cache

import (
	"context"
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httputil"
	"net/url"
	"strings"
	"time"
)

type Server struct {
	config     Config
	logger     *slog.Logger
	repository Repository
	upstream   Upstream
	now        func() time.Time
	proxy      *httputil.ReverseProxy
}

func NewServer(config Config, repository Repository, upstream Upstream, logger *slog.Logger) *Server {
	if logger == nil {
		logger = slog.Default()
	}
	server := &Server{
		config:     config,
		logger:     logger,
		repository: repository,
		upstream:   upstream,
		now:        func() time.Time { return time.Now().UTC() },
	}
	if strings.TrimSpace(config.DotnetUpstream) != "" {
		server.proxy = newReverseProxy(config.DotnetUpstream, logger)
	}
	return server
}

func (server *Server) ServeHTTP(response http.ResponseWriter, request *http.Request) {
	switch {
	case request.Method == http.MethodGet && request.URL.Path == "/health":
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "go-cache-service", "timestamp": server.now()})
	case request.Method == http.MethodGet && strings.HasPrefix(request.URL.Path, "/api/v1/cache/costs/city/"):
		server.handleGetCityCost(response, request)
	case request.Method == http.MethodPost && request.URL.Path == "/api/v1/cache/costs/city/batch":
		server.handleBatchCityCosts(response, request)
	case request.Method == http.MethodGet && strings.HasPrefix(request.URL.Path, "/api/v1/cache/scores/city/"):
		server.handleGetCityScore(response, request)
	case request.Method == http.MethodPost && request.URL.Path == "/api/v1/cache/scores/city/batch":
		server.handleBatchCityScores(response, request)
	case request.Method == http.MethodGet && strings.HasPrefix(request.URL.Path, "/api/v1/cache/scores/coworking/"):
		server.handleGetCoworkingScore(response, request)
	case request.Method == http.MethodPost && request.URL.Path == "/api/v1/cache/scores/coworking/batch":
		server.handleBatchCoworkingScores(response, request)
	case pathMatchesPrefix(request.URL.Path, "/api/v1/cache") && server.proxy != nil:
		server.proxy.ServeHTTP(response, request)
	default:
		writeJSON(response, http.StatusNotFound, map[string]any{"error": "No route matched"})
	}
}

func (server *Server) handleGetCityCost(response http.ResponseWriter, request *http.Request) {
	cityID := strings.TrimPrefix(request.URL.Path, "/api/v1/cache/costs/city/")
	if cityID == "" || strings.Contains(cityID, "/") {
		writeJSON(response, http.StatusNotFound, map[string]any{"error": "Failed to get city cost"})
		return
	}
	ctx := request.Context()
	now := server.now()
	if entry, ok := server.repository.GetCost(ctx, CostEntityCity, cityID); ok && !entry.expired(now) {
		writeJSON(response, http.StatusOK, CostResponse{EntityID: cityID, AverageCost: entry.AverageCost, FromCache: true, Statistics: entry.Statistics})
		return
	}
	result, err := server.upstream.GetCityCost(ctx, cityID)
	if err != nil {
		server.logger.Error("failed to get city cost", "cityId", cityID, "error", err)
		writeJSON(response, http.StatusInternalServerError, map[string]any{"error": "Failed to get city cost"})
		return
	}
	entry := costCacheData{AverageCost: result.AverageCost, CreatedAt: now, ExpiresAt: now.Add(server.config.CostTTL), Statistics: result.Statistics}
	server.repository.SetCost(ctx, CostEntityCity, cityID, entry)
	writeJSON(response, http.StatusOK, result)
}

func (server *Server) handleBatchCityCosts(response http.ResponseWriter, request *http.Request) {
	cityIDs, ok := readStringArray(response, request, "City IDs are required")
	if !ok {
		return
	}
	ctx := request.Context()
	now := server.now()
	result := BatchCostResponse{TotalCount: len(cityIDs)}
	missing := make([]string, 0, len(cityIDs))
	cached := server.repository.GetCostsBatch(ctx, CostEntityCity, cityIDs)
	for _, cityID := range cityIDs {
		if entry, ok := cached[cityID]; ok && !entry.expired(now) {
			result.Costs = append(result.Costs, CostResponse{EntityID: cityID, AverageCost: entry.AverageCost, FromCache: true, Statistics: entry.Statistics})
			result.CachedCount++
		} else {
			missing = append(missing, cityID)
		}
	}
	if len(missing) > 0 {
		calculated, err := server.upstream.GetCityCostsBatch(ctx, missing)
		if err != nil {
			server.logger.Error("failed to get batch city costs", "error", err)
			writeJSON(response, http.StatusInternalServerError, map[string]any{"error": "Failed to get batch city costs"})
			return
		}
		entries := make(map[string]costCacheData, len(calculated))
		for _, item := range calculated {
			result.Costs = append(result.Costs, item)
			result.CalculatedCount++
			entries[item.EntityID] = costCacheData{AverageCost: item.AverageCost, CreatedAt: now, ExpiresAt: now.Add(server.config.CostTTL), Statistics: item.Statistics}
		}
		server.repository.SetCosts(ctx, CostEntityCity, entries)
	}
	writeJSON(response, http.StatusOK, result)
}

func (server *Server) handleGetCityScore(response http.ResponseWriter, request *http.Request) {
	cityID := strings.TrimPrefix(request.URL.Path, "/api/v1/cache/scores/city/")
	if cityID == "" || strings.Contains(cityID, "/") {
		writeJSON(response, http.StatusNotFound, map[string]any{"error": "Failed to get city score"})
		return
	}
	ctx := request.Context()
	now := server.now()
	if entry, ok := server.repository.GetScore(ctx, ScoreEntityCity, cityID); ok && !entry.expired(now) {
		writeJSON(response, http.StatusOK, ScoreResponse{EntityID: cityID, OverallScore: entry.OverallScore, FromCache: true, Statistics: entry.Statistics})
		return
	}
	result, err := server.upstream.GetCityScore(ctx, cityID)
	if err != nil {
		server.logger.Error("failed to get city score", "cityId", cityID, "error", err)
		writeJSON(response, http.StatusInternalServerError, map[string]any{"error": "Failed to get city score"})
		return
	}
	if result.OverallScore > 0 {
		server.repository.SetScore(ctx, ScoreEntityCity, cityID, scoreCacheData{OverallScore: result.OverallScore, CreatedAt: now, ExpiresAt: now.Add(server.config.ScoreTTL), Statistics: result.Statistics})
	}
	writeJSON(response, http.StatusOK, result)
}

func (server *Server) handleBatchCityScores(response http.ResponseWriter, request *http.Request) {
	cityIDs, ok := readStringArray(response, request, "City IDs are required")
	if !ok {
		return
	}
	ctx := request.Context()
	now := server.now()
	result := BatchScoreResponse{TotalCount: len(cityIDs)}
	missing := make([]string, 0, len(cityIDs))
	cached := server.repository.GetScoresBatch(ctx, ScoreEntityCity, cityIDs)
	for _, cityID := range cityIDs {
		if entry, ok := cached[cityID]; ok && !entry.expired(now) {
			result.Scores = append(result.Scores, ScoreResponse{EntityID: cityID, OverallScore: entry.OverallScore, FromCache: true, Statistics: entry.Statistics})
			result.CachedCount++
		} else {
			missing = append(missing, cityID)
		}
	}
	if len(missing) > 0 {
		calculated, err := server.upstream.GetCityScoresBatch(ctx, missing)
		if err != nil {
			server.logger.Error("failed to get batch city scores", "error", err)
			writeJSON(response, http.StatusInternalServerError, map[string]any{"error": "Failed to get batch city scores"})
			return
		}
		entries := make(map[string]scoreCacheData)
		for _, item := range calculated {
			result.Scores = append(result.Scores, item)
			result.CalculatedCount++
			if item.OverallScore > 0 {
				entries[item.EntityID] = scoreCacheData{OverallScore: item.OverallScore, CreatedAt: now, ExpiresAt: now.Add(server.config.ScoreTTL), Statistics: item.Statistics}
			}
		}
		server.repository.SetScores(ctx, ScoreEntityCity, entries)
	}
	writeJSON(response, http.StatusOK, result)
}

func (server *Server) handleGetCoworkingScore(response http.ResponseWriter, request *http.Request) {
	coworkingID := strings.TrimPrefix(request.URL.Path, "/api/v1/cache/scores/coworking/")
	if coworkingID == "" || strings.Contains(coworkingID, "/") {
		writeJSON(response, http.StatusNotFound, map[string]any{"error": "Failed to get coworking score"})
		return
	}
	ctx := request.Context()
	now := server.now()
	if entry, ok := server.repository.GetScore(ctx, ScoreEntityCoworking, coworkingID); ok && !entry.expired(now) {
		writeJSON(response, http.StatusOK, ScoreResponse{EntityID: coworkingID, OverallScore: entry.OverallScore, FromCache: true, Statistics: entry.Statistics})
		return
	}
	result, err := server.upstream.GetCoworkingScore(ctx, coworkingID)
	if err != nil {
		server.logger.Error("failed to get coworking score", "coworkingId", coworkingID, "error", err)
		writeJSON(response, http.StatusInternalServerError, map[string]any{"error": "Failed to get coworking score"})
		return
	}
	server.repository.SetScore(ctx, ScoreEntityCoworking, coworkingID, scoreCacheData{OverallScore: result.OverallScore, CreatedAt: now, ExpiresAt: now.Add(server.config.ScoreTTL), Statistics: result.Statistics})
	writeJSON(response, http.StatusOK, result)
}

func (server *Server) handleBatchCoworkingScores(response http.ResponseWriter, request *http.Request) {
	coworkingIDs, ok := readStringArray(response, request, "Coworking IDs are required")
	if !ok {
		return
	}
	ctx := request.Context()
	now := server.now()
	result := BatchScoreResponse{TotalCount: len(coworkingIDs)}
	missing := make([]string, 0, len(coworkingIDs))
	cached := server.repository.GetScoresBatch(ctx, ScoreEntityCoworking, coworkingIDs)
	for _, coworkingID := range coworkingIDs {
		if entry, ok := cached[coworkingID]; ok && !entry.expired(now) {
			result.Scores = append(result.Scores, ScoreResponse{EntityID: coworkingID, OverallScore: entry.OverallScore, FromCache: true, Statistics: entry.Statistics})
			result.CachedCount++
		} else {
			missing = append(missing, coworkingID)
		}
	}
	if len(missing) > 0 {
		calculated, err := server.upstream.GetCoworkingScoresBatch(ctx, missing)
		if err != nil {
			server.logger.Error("failed to get batch coworking scores", "error", err)
			writeJSON(response, http.StatusInternalServerError, map[string]any{"error": "Failed to get batch coworking scores"})
			return
		}
		entries := make(map[string]scoreCacheData, len(calculated))
		for _, item := range calculated {
			result.Scores = append(result.Scores, item)
			result.CalculatedCount++
			entries[item.EntityID] = scoreCacheData{OverallScore: item.OverallScore, CreatedAt: now, ExpiresAt: now.Add(server.config.ScoreTTL), Statistics: item.Statistics}
		}
		server.repository.SetScores(ctx, ScoreEntityCoworking, entries)
	}
	writeJSON(response, http.StatusOK, result)
}

func readStringArray(response http.ResponseWriter, request *http.Request, emptyMessage string) ([]string, bool) {
	var values []string
	if err := json.NewDecoder(request.Body).Decode(&values); err != nil {
		writeJSON(response, http.StatusBadRequest, map[string]any{"error": emptyMessage})
		return nil, false
	}
	if len(values) == 0 {
		writeJSON(response, http.StatusBadRequest, map[string]any{"error": emptyMessage})
		return nil, false
	}
	return values, true
}

func newReverseProxy(rawURL string, logger *slog.Logger) *httputil.ReverseProxy {
	target, err := url.Parse(rawURL)
	if err != nil {
		panic(err)
	}
	proxy := httputil.NewSingleHostReverseProxy(target)
	proxy.ErrorHandler = func(response http.ResponseWriter, request *http.Request, proxyErr error) {
		if logger != nil {
			logger.Error("cache fallback proxy failed", "error", proxyErr)
		}
		writeJSON(response, http.StatusBadGateway, map[string]any{"error": "Failed to proxy cache request"})
	}
	return proxy
}

func pathMatchesPrefix(path string, prefix string) bool {
	path = strings.TrimRight(path, "/")
	prefix = strings.TrimRight(prefix, "/")
	if path == prefix {
		return true
	}
	return strings.HasPrefix(path, prefix+"/")
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}

func withTimeout(parent context.Context, timeout time.Duration) (context.Context, context.CancelFunc) {
	if timeout <= 0 {
		return context.WithCancel(parent)
	}
	return context.WithTimeout(parent, timeout)
}
