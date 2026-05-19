package gateway

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
)

func TestProductCanaryRouteProxiesPublicListToGoUpstream(t *testing.T) {
	goProductUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{
			"source": "go-product",
			"path":   request.URL.Path,
		})
	}))
	defer goProductUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["go-product-service"] = goProductUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/products", Value: "go-product-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/products?page=1&pageSize=10", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-product" || payload["path"] != "/api/v1/products" {
		t.Fatalf("expected go product upstream, got %+v", payload)
	}
}

func TestProductCanaryRouteCoversUserList(t *testing.T) {
	goProductUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{
			"source": "go-product",
			"path":   request.URL.Path,
		})
	}))
	defer goProductUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["go-product-service"] = goProductUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/products", Value: "go-product-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/products/user/1?page=1&pageSize=10", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-product" || payload["path"] != "/api/v1/products/user/1" {
		t.Fatalf("expected user list to use go product upstream, got %+v", payload)
	}
}

func TestProductWriteRouteRequiresAuth(t *testing.T) {
	upstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		response.WriteHeader(http.StatusOK)
	}))
	defer upstream.Close()

	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.ServiceURLs["product-service"] = upstream.URL

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/products", strings.NewReader(`{"name":"Phone"}`))

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d: %s", recorder.Code, recorder.Body.String())
	}
}
