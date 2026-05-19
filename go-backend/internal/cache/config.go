package cache

import (
	"os"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	ListenAddress         string
	RedisConnectionString string
	DotnetUpstream        string
	CityServiceURL        string
	CoworkingServiceURL   string
	RequestTimeout        time.Duration
	ScoreTTL              time.Duration
	CostTTL               time.Duration
}

func LoadConfigFromEnv() Config {
	return Config{
		ListenAddress:         envOrDefault("GO_CACHE_LISTEN_ADDRESS", envOrDefault("HTTP_ADDR", ":5210")),
		RedisConnectionString: firstEnv("REDIS_CONNECTION_STRING", "Redis__ConnectionString", "ConnectionStrings__Redis"),
		DotnetUpstream:        firstEnv("GO_CACHE_DOTNET_UPSTREAM"),
		CityServiceURL:        firstEnv("ServiceUrls__CityService"),
		CoworkingServiceURL:   firstEnv("ServiceUrls__CoworkingService"),
		RequestTimeout:        durationEnvOrDefault("GO_CACHE_REQUEST_TIMEOUT", 5*time.Second),
		ScoreTTL:              durationEnvOrDefault("GO_CACHE_SCORE_TTL", 24*time.Hour),
		CostTTL:               durationEnvOrDefault("GO_CACHE_COST_TTL", 24*time.Hour),
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

func intEnvOrDefault(key string, fallback int) int {
	value := strings.TrimSpace(os.Getenv(key))
	if value == "" {
		return fallback
	}
	parsed, err := strconv.Atoi(value)
	if err != nil || parsed <= 0 {
		return fallback
	}
	return parsed
}

func durationEnvOrDefault(key string, fallback time.Duration) time.Duration {
	value := strings.TrimSpace(os.Getenv(key))
	if value == "" {
		return fallback
	}
	parsed, err := time.ParseDuration(value)
	if err != nil || parsed <= 0 {
		return fallback
	}
	return parsed
}
