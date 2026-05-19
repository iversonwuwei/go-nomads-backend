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

func TestHealth(t *testing.T) {
	server := NewServer(DefaultConfig(), slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/health", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", recorder.Code)
	}
}

func TestProxyAddsUserHeaders(t *testing.T) {
	upstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]string{
			"userId": request.Header.Get("X-User-Id"),
			"method": request.Method,
		})
	}))
	defer upstream.Close()

	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.ServiceURLs["user-service"] = upstream.URL
	server := NewServer(config, slog.Default())
	server.now = func() time.Time { return time.Unix(1_700_000_000, 0) }

	token := signedToken(t, config.JWTSecret, map[string]any{
		"sub": "user-1",
		"exp": server.now().Add(time.Hour).Unix(),
	})

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/users/me", nil)
	request.Header.Set("Authorization", "Bearer "+token)
	request.Header.Set("X-HTTP-Method-Override", "PUT")

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload map[string]string
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["userId"] != "user-1" {
		t.Fatalf("expected user header to be forwarded, got %+v", payload)
	}
	if payload["method"] != http.MethodPut {
		t.Fatalf("expected method override, got %+v", payload)
	}
}

func TestConfigCanaryRouteProxiesPublicConfigToGoUpstream(t *testing.T) {
	goConfigUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{
			"source": "go-config",
			"path":   request.URL.Path,
		})
	}))
	defer goConfigUpstream.Close()

	dotnetConfigUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{
			"source": "dotnet-config",
			"path":   request.URL.Path,
		})
	}))
	defer dotnetConfigUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["config-service"] = dotnetConfigUpstream.URL
	config.ServiceURLs["go-config-service"] = goConfigUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/app/config", Value: "go-config-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config?locale=zh-CN", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-config" {
		t.Fatalf("expected go canary upstream, got %+v", payload)
	}
	if payload["path"] != "/api/v1/app/config" {
		t.Fatalf("expected public config path to be preserved, got %+v", payload)
	}
}

func TestConfigCanaryRouteAlsoCoversVersionEndpoint(t *testing.T) {
	goConfigUpstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		_ = json.NewEncoder(response).Encode(map[string]any{
			"source": "go-config",
			"path":   request.URL.Path,
		})
	}))
	defer goConfigUpstream.Close()

	config := DefaultConfig()
	config.ServiceURLs["go-config-service"] = goConfigUpstream.URL
	config.Routes = applyRouteTargets(config.Routes, []assignment{{Key: "/api/v1/app/config", Value: "go-config-service"}})

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config/version", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload map[string]any
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload["source"] != "go-config" || payload["path"] != "/api/v1/app/config/version" {
		t.Fatalf("expected version endpoint to use go canary upstream, got %+v", payload)
	}
}

func TestAdminConfigRouteRequiresAuth(t *testing.T) {
	upstream := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		response.WriteHeader(http.StatusOK)
	}))
	defer upstream.Close()

	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.ServiceURLs["config-service"] = upstream.URL

	server := NewServer(config, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/admin/config/snapshots", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d: %s", recorder.Code, recorder.Body.String())
	}
	if !strings.Contains(recorder.Body.String(), "Invalid or expired token") {
		t.Fatalf("expected unauthorized payload, got %s", recorder.Body.String())
	}
}
