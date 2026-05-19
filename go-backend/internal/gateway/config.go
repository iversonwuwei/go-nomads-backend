package gateway

import (
	"os"
	"strings"
	"time"
)

type Config struct {
	ListenAddress  string
	JWTSecret      string
	JWTIssuer      string
	JWTAudience    string
	ProxyTimeout   time.Duration
	ServiceURLs    map[string]string
	Routes         []Route
	PublicPaths    []string
	PublicGetPaths []string
	AdminPrefixes  []string
}

type Route struct {
	ServiceName string
	Prefix      string
	Order       int
}

type assignment struct {
	Key   string
	Value string
}

func LoadConfigFromEnv() Config {
	config := DefaultConfig()
	config.ListenAddress = envOrDefault("GO_GATEWAY_LISTEN_ADDRESS", envOrDefault("HTTP_ADDR", ":5081"))
	config.JWTSecret = firstEnv("Jwt__Secret", "JWT_SECRET")
	config.JWTIssuer = firstEnv("Jwt__Issuer", "JWT_ISSUER")
	config.JWTAudience = firstEnv("Jwt__Audience", "JWT_AUDIENCE")

	for envName, serviceName := range serviceURLKeys() {
		if value := strings.TrimSpace(os.Getenv(envName)); value != "" {
			config.ServiceURLs[serviceName] = value
		}
	}

	for _, upstream := range parseAssignments(firstEnv("GO_GATEWAY_UPSTREAMS", "GO_GATEWAY_SERVICE_URLS")) {
		config.ServiceURLs[upstream.Key] = upstream.Value
	}

	config.Routes = applyRouteTargets(config.Routes, parseAssignments(firstEnv("GO_GATEWAY_ROUTE_TARGETS", "GO_GATEWAY_ROUTE_OVERRIDES")))

	return config
}

func parseAssignments(value string) []assignment {
	parts := strings.FieldsFunc(value, func(character rune) bool {
		return character == ',' || character == ';' || character == '\n' || character == '\r'
	})

	assignments := make([]assignment, 0, len(parts))
	for _, part := range parts {
		key, val, ok := strings.Cut(part, "=")
		if !ok {
			continue
		}

		key = strings.TrimSpace(key)
		val = strings.TrimSpace(val)
		if key == "" || val == "" {
			continue
		}

		assignments = append(assignments, assignment{Key: key, Value: val})
	}

	return assignments
}

func applyRouteTargets(routes []Route, targets []assignment) []Route {
	if len(targets) == 0 {
		return routes
	}

	updated := append([]Route(nil), routes...)
	for index, target := range targets {
		applied := false
		for routeIndex := range updated {
			if strings.TrimRight(updated[routeIndex].Prefix, "/") == strings.TrimRight(target.Key, "/") {
				updated[routeIndex].ServiceName = target.Value
				updated[routeIndex].Order = -10_000 + index
				applied = true
				break
			}
		}

		if !applied {
			updated = append(updated, Route{ServiceName: target.Value, Prefix: target.Key, Order: -10_000 + index})
		}
	}

	return updated
}

func DefaultConfig() Config {
	serviceURLs := map[string]string{
		"user-service":          "http://user-service:5001",
		"city-service":          "http://city-service:5202",
		"coworking-service":     "http://coworking-service:5203",
		"event-service":         "http://event-service:5205",
		"ai-image-service":      "http://ai-service:5209",
		"ai-service":            "http://ai-service:5209",
		"cache-service":         "http://cache-service:5210",
		"message-service":       "http://message-service:5005",
		"innovation-service":    "http://innovation-service:5206",
		"search-service":        "http://search-service:5215",
		"accommodation-service": "http://accommodation-service:5204",
		"product-service":       "http://product-service:5002",
		"config-service":        "http://config-service:5213",
	}

	return Config{
		ListenAddress:  ":5081",
		ProxyTimeout:   10 * time.Minute,
		ServiceURLs:    serviceURLs,
		Routes:         defaultRoutes(),
		PublicPaths:    defaultPublicPaths(),
		PublicGetPaths: defaultPublicGetPaths(),
		AdminPrefixes:  []string{"/api/users/admin", "/api/v1/admin/", "/api/v1/reports"},
	}
}

func serviceURLKeys() map[string]string {
	return map[string]string{
		"ServiceUrls__UserService":          "user-service",
		"ServiceUrls__CityService":          "city-service",
		"ServiceUrls__CoworkingService":     "coworking-service",
		"ServiceUrls__EventService":         "event-service",
		"ServiceUrls__AIImageService":       "ai-image-service",
		"ServiceUrls__AIService":            "ai-service",
		"ServiceUrls__CacheService":         "cache-service",
		"ServiceUrls__MessageService":       "message-service",
		"ServiceUrls__InnovationService":    "innovation-service",
		"ServiceUrls__SearchService":        "search-service",
		"ServiceUrls__AccommodationService": "accommodation-service",
		"ServiceUrls__ProductService":       "product-service",
		"ServiceUrls__ConfigService":        "config-service",
	}
}

func defaultPublicPaths() []string {
	return []string{
		"/api/v1/auth/login",
		"/api/v1/auth/register",
		"/api/v1/auth/forgot-password",
		"/api/v1/auth/refresh",
		"/api/v1/auth/logout",
		"/api/v1/auth/social-login",
		"/api/v1/auth/alipay/auth-info",
		"/api/v1/auth/sms/send",
		"/api/v1/auth/sms/login",
		"/api/users/login",
		"/api/users/register",
		"/api/users/refresh",
		"/api/test",
		"/health",
		"/metrics",
		"/scalar/v1",
	}
}

func defaultPublicGetPaths() []string {
	return []string{
		"/api/v1/cities",
		"/api/v1/hotels",
		"/api/v1/coworking",
		"/api/v1/products",
		"/api/v1/search",
		"/api/v1/index",
		"/api/v1/users/legal",
		"/api/v1/app/config",
	}
}

func envOrDefault(key string, fallback string) string {
	if value := strings.TrimSpace(os.Getenv(key)); value != "" {
		return value
	}

	return fallback
}

func firstEnv(keys ...string) string {
	for _, key := range keys {
		if value := strings.TrimSpace(os.Getenv(key)); value != "" {
			return value
		}
	}

	return ""
}
