package cache

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"os"
	"reflect"
	"strings"
	"testing"
	"time"
)

func TestGetCityCostReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cache/costs/city/city-1", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "get_city_cost_response.json"), recorder.Body.Bytes())
}

func TestGetCityCostsBatchReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/cache/costs/city/batch", stringsReader(`["city-1","city-2"]`))
	request.Header.Set("Content-Type", "application/json")

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "batch_city_costs_response.json"), recorder.Body.Bytes())
}

func TestGetCityScoreReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cache/scores/city/city-1", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "get_city_score_response.json"), recorder.Body.Bytes())
}

func TestGetCityScoresBatchReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/cache/scores/city/batch", stringsReader(`["city-1","city-2"]`))
	request.Header.Set("Content-Type", "application/json")

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "batch_city_scores_response.json"), recorder.Body.Bytes())
}

func TestGetCoworkingScoreReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cache/scores/coworking/cow-1", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "get_coworking_score_response.json"), recorder.Body.Bytes())
}

func TestGetCoworkingScoresBatchReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/cache/scores/coworking/batch", stringsReader(`["cow-1","cow-2"]`))
	request.Header.Set("Content-Type", "application/json")

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "batch_coworking_scores_response.json"), recorder.Body.Bytes())
}

func TestGetCityCostReturnsCachedResponseOnSecondRequest(t *testing.T) {
	server := newTestServer()
	first := httptest.NewRecorder()
	server.ServeHTTP(first, httptest.NewRequest(http.MethodGet, "/api/v1/cache/costs/city/city-1", nil))

	second := httptest.NewRecorder()
	server.ServeHTTP(second, httptest.NewRequest(http.MethodGet, "/api/v1/cache/costs/city/city-1", nil))

	if second.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", second.Code, second.Body.String())
	}
	var payload CostResponse
	if err := json.Unmarshal(second.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if !payload.FromCache {
		t.Fatalf("expected cached response, got %+v", payload)
	}
}

func TestWritePathsProxyToDotnetCacheService(t *testing.T) {
	proxied := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "dotnet-cache", "path": request.URL.Path, "method": request.Method})
	}))
	defer proxied.Close()

	server := NewServer(
		Config{ListenAddress: ":5210", DotnetUpstream: proxied.URL, ScoreTTL: 24 * time.Hour, CostTTL: 24 * time.Hour},
		NewMemoryRepository(),
		&StubUpstream{},
		slog.Default(),
	)

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPut, "/api/v1/cache/scores/city/city-1", stringsReader(`{"overallScore":4.2}`))
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "dotnet-cache" || payload["path"] != "/api/v1/cache/scores/city/city-1" {
		t.Fatalf("expected dotnet cache proxy fallback, got %+v", payload)
	}
}

func newTestServer() *Server {
	upstream := &StubUpstream{
		CityScores: map[string]ScoreResponse{
			"city-1": {EntityID: "city-1", OverallScore: 4.25, Statistics: ptr(`[{"category":"safety","averageRating":4.5,"ratingCount":10},{"category":"internet","averageRating":4,"ratingCount":8}]`)},
			"city-2": {EntityID: "city-2", OverallScore: 0},
		},
		CityCosts: map[string]CostResponse{
			"city-1": {EntityID: "city-1", AverageCost: 123.45, Statistics: ptr(`"{\"totalAverageCost\":123.45,\"categoryCosts\":{\"accommodation\":70,\"food\":30},\"contributorCount\":4,\"totalExpenseCount\":12,\"currency\":\"USD\",\"updatedAt\":\"2026-05-07T09:00:00Z\"}"`)},
			"city-2": {EntityID: "city-2", AverageCost: 88.5, Statistics: ptr(`"{\"totalAverageCost\":88.5,\"categoryCosts\":{\"accommodation\":50,\"food\":20},\"contributorCount\":2,\"totalExpenseCount\":6,\"currency\":\"USD\",\"updatedAt\":\"2026-05-07T09:05:00Z\"}"`)},
		},
		CoworkingScores: map[string]ScoreResponse{
			"cow-1": {EntityID: "cow-1", OverallScore: 4.8, Statistics: ptr(`{"ReviewCount":7}`)},
			"cow-2": {EntityID: "cow-2", OverallScore: 4.1, Statistics: ptr(`{"ReviewCount":3}`)},
		},
	}
	server := NewServer(Config{ListenAddress: ":5210", ScoreTTL: 24 * time.Hour, CostTTL: 24 * time.Hour}, NewMemoryRepository(), upstream, slog.Default())
	server.now = func() time.Time { return fixedTime("2026-05-07T10:00:00Z") }
	return server
}

func stringsReader(value string) *strings.Reader {
	return strings.NewReader(value)
}

func readTestdata(t *testing.T, fileName string) []byte {
	t.Helper()
	content, err := os.ReadFile("testdata/" + fileName)
	if err != nil {
		t.Fatal(err)
	}
	return content
}

func assertJSONEqual(t *testing.T, expected []byte, actual []byte) {
	t.Helper()
	var expectedValue any
	if err := json.Unmarshal(expected, &expectedValue); err != nil {
		t.Fatal(err)
	}
	var actualValue any
	if err := json.Unmarshal(actual, &actualValue); err != nil {
		t.Fatal(err)
	}
	if !reflect.DeepEqual(expectedValue, actualValue) {
		t.Fatalf("json mismatch\nexpected: %s\nactual:   %s", expected, actual)
	}
}
