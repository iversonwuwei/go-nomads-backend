package gateway

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestSearchCanaryRouteProxiesPublicSearchToGoUpstream(t *testing.T) {
	goSearchUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "go-search", "path": request.URL.Path})
	}))
	defer goSearchUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["go-search-service"] = goSearchUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/search", Value: "go-search-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/search?query=nomad&type=all&page=1&pageSize=20", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-search" || payload["path"] != "/api/v1/search" {
		t.Fatalf("expected go search upstream, got %+v", payload)
	}
}

func TestIndexRouteStaysOnDotnetWhenSearchCanaryEnabled(t *testing.T) {
	dotnetSearchUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "dotnet-search", "path": request.URL.Path})
	}))
	defer dotnetSearchUpstream.Close()

	goSearchUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "go-search", "path": request.URL.Path})
	}))
	defer goSearchUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["search-service"] = dotnetSearchUpstream.URL
	config.ServiceURLs["go-search-service"] = goSearchUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/search", Value: "go-search-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/index/health", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "dotnet-search" || payload["path"] != "/api/v1/index/health" {
		t.Fatalf("expected index route to remain on dotnet search, got %+v", payload)
	}
}
