package gateway

import (
	"context"
	"encoding/json"
	"log/slog"
	"net"
	"net/http"
	"net/http/httputil"
	"net/url"
	"strings"
	"time"
)

type Server struct {
	config  Config
	logger  *slog.Logger
	proxies map[string]http.Handler
	now     func() time.Time
}

func NewServer(config Config, logger *slog.Logger) *Server {
	if logger == nil {
		logger = slog.Default()
	}

	server := &Server{
		config:  config,
		logger:  logger,
		proxies: make(map[string]http.Handler),
		now:     time.Now,
	}

	for serviceName, rawURL := range config.ServiceURLs {
		parsedURL, err := url.Parse(rawURL)
		if err != nil {
			logger.Warn("invalid upstream url", "service", serviceName, "url", rawURL, "error", err)
			continue
		}

		server.proxies[serviceName] = newReverseProxy(parsedURL, config.ProxyTimeout)
	}

	return server
}

func (server *Server) ServeHTTP(response http.ResponseWriter, request *http.Request) {
	if request.URL.Path == "/health" {
		writeJSON(response, http.StatusOK, map[string]any{
			"status":    "healthy",
			"service":   "go-gateway",
			"timestamp": server.now().UTC(),
		})
		return
	}

	applyMethodOverride(request)

	if !isPublicPath(request.URL.Path, request.Method, server.config.PublicPaths, server.config.PublicGetPaths) && strings.HasPrefix(request.URL.Path, "/api/") {
		claims, err := validateBearerToken(request.Header.Get("Authorization"), server.config, server.now())
		if err != nil {
			server.logger.Warn("jwt validation failed", "path", request.URL.Path, "error", err)
			writeJSON(response, http.StatusUnauthorized, map[string]any{
				"success": false,
				"message": "Invalid or expired token",
				"error":   "Unauthorized",
			})
			return
		}

		if claims.Subject != "" {
			request.Header.Set("X-User-Id", claims.Subject)
		}
		if claims.Email != "" {
			request.Header.Set("X-User-Email", claims.Email)
		}
		if claims.Role != "" {
			request.Header.Set("X-User-Role", claims.Role)
		}
	}

	route, ok := matchRoute(request.URL.Path, server.config.Routes)
	if !ok {
		writeJSON(response, http.StatusNotFound, map[string]any{
			"success": false,
			"message": "No route matched",
			"error":   "Not Found",
		})
		return
	}

	proxy, ok := server.proxies[route.ServiceName]
	if !ok {
		writeJSON(response, http.StatusBadGateway, map[string]any{
			"success": false,
			"message": "Upstream service is not configured",
			"error":   "Bad Gateway",
		})
		return
	}

	request.Header.Set("X-Forwarded-Host", request.Host)
	request.Header.Set("X-Forwarded-Proto", forwardedProto(request))
	request.Header.Set("X-Forwarded-For", forwardedFor(request))
	request.Header.Set("X-Go-Nomads-Gateway", "go")

	server.logger.Info("proxy request", "method", request.Method, "path", request.URL.Path, "service", route.ServiceName)
	proxy.ServeHTTP(response, request)
}

func newReverseProxy(target *url.URL, timeout time.Duration) http.Handler {
	proxy := httputil.NewSingleHostReverseProxy(target)
	originalDirector := proxy.Director
	proxy.Director = func(request *http.Request) {
		originalDirector(request)
		request.Host = target.Host
	}
	proxy.Transport = &http.Transport{
		Proxy:                 http.ProxyFromEnvironment,
		DialContext:           (&net.Dialer{Timeout: 30 * time.Second, KeepAlive: 30 * time.Second}).DialContext,
		ForceAttemptHTTP2:     true,
		MaxIdleConns:          100,
		IdleConnTimeout:       90 * time.Second,
		TLSHandshakeTimeout:   10 * time.Second,
		ExpectContinueTimeout: 1 * time.Second,
		ResponseHeaderTimeout: timeout,
	}
	proxy.ErrorHandler = func(response http.ResponseWriter, request *http.Request, err error) {
		writeJSON(response, http.StatusBadGateway, map[string]any{
			"success": false,
			"message": "Upstream request failed",
			"error":   err.Error(),
		})
	}

	return proxy
}

func applyMethodOverride(request *http.Request) {
	if !strings.EqualFold(request.Method, http.MethodPost) {
		return
	}

	override := strings.ToUpper(strings.TrimSpace(request.Header.Get("X-HTTP-Method-Override")))
	switch override {
	case http.MethodPut, http.MethodPatch, http.MethodDelete:
		request.Method = override
	}
}

func forwardedProto(request *http.Request) string {
	if value := request.Header.Get("X-Forwarded-Proto"); value != "" {
		return value
	}
	if request.TLS != nil {
		return "https"
	}
	return "http"
}

func forwardedFor(request *http.Request) string {
	current := request.Header.Get("X-Forwarded-For")
	host, _, err := net.SplitHostPort(request.RemoteAddr)
	if err != nil {
		host = request.RemoteAddr
	}
	if current == "" {
		return host
	}
	return current + ", " + host
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}

func contextWithTimeout(parent context.Context, timeout time.Duration) (context.Context, context.CancelFunc) {
	if timeout <= 0 {
		return context.WithCancel(parent)
	}
	return context.WithTimeout(parent, timeout)
}
