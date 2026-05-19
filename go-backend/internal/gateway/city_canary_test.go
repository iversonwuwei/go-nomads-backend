package gateway

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestCityRegionTabsCanaryRouteProxiesToGoUpstream(t *testing.T) {
	goCityUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "go-city", "path": request.URL.Path})
	}))
	defer goCityUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["go-city-service"] = goCityUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/cities/region-tabs", Value: "go-city-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cities/region-tabs", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-city" || payload["path"] != "/api/v1/cities/region-tabs" {
		t.Fatalf("expected go city upstream, got %+v", payload)
	}
}

func TestCityListRouteStaysOnDotnetWhenRegionTabsCanaryEnabled(t *testing.T) {
	dotnetCityUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "dotnet-city", "path": request.URL.Path})
	}))
	defer dotnetCityUpstream.Close()

	goCityUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "go-city", "path": request.URL.Path})
	}))
	defer goCityUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["city-service"] = dotnetCityUpstream.URL
	config.ServiceURLs["go-city-service"] = goCityUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/cities/region-tabs", Value: "go-city-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cities/list", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "dotnet-city" || payload["path"] != "/api/v1/cities/list" {
		t.Fatalf("expected city list route to remain on dotnet city, got %+v", payload)
	}
}
