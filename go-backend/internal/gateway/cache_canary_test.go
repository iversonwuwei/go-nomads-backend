package gateway

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"
)

func TestCacheCanaryRouteProxiesProtectedReadToGoUpstream(t *testing.T) {
	goCacheUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "go-cache", "path": request.URL.Path, "method": request.Method})
	}))
	defer goCacheUpstream.Close()

	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.ServiceURLs["go-cache-service"] = goCacheUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/cache", Value: "go-cache-service"}})

	server := NewServer(config, slog.Default())
	token := signedToken(t, config.JWTSecret, map[string]any{"sub": "user-1", "exp": time.Now().Add(time.Hour).Unix()})
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cache/scores/city/city-1", nil)
	request.Header.Set("Authorization", "Bearer "+token)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-cache" || payload["path"] != "/api/v1/cache/scores/city/city-1" {
		t.Fatalf("expected go cache canary upstream, got %+v", payload)
	}
}

func TestCacheCanaryPrefixAlsoRoutesWritePathToGoUpstream(t *testing.T) {
	goCacheUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{"source": "go-cache", "path": request.URL.Path, "method": request.Method})
	}))
	defer goCacheUpstream.Close()

	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.ServiceURLs["go-cache-service"] = goCacheUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/cache", Value: "go-cache-service"}})

	server := NewServer(config, slog.Default())
	token := signedToken(t, config.JWTSecret, map[string]any{"sub": "user-1", "exp": time.Now().Add(time.Hour).Unix()})
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPut, "/api/v1/cache/scores/city/city-1", strings.NewReader(`{"overallScore":4.2}`))
	request.Header.Set("Authorization", "Bearer "+token)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-cache" || payload["method"] != http.MethodPut {
		t.Fatalf("expected write path to route through go cache canary upstream, got %+v", payload)
	}
}

func TestCacheWriteRouteRequiresAuth(t *testing.T) {
	upstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		response.WriteHeader(http.StatusOK)
	}))
	defer upstream.Close()

	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.ServiceURLs["cache-service"] = upstream.URL

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPut, "/api/v1/cache/scores/city/city-1", strings.NewReader(`{"overallScore":4.2}`))

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d: %s", recorder.Code, recorder.Body.String())
	}
}
