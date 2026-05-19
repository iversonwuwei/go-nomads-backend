package city

import (
	"os"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	ListenAddress            string
	PostgresConnectionString string
	DotnetUpstream           string
	QueryTimeout             time.Duration
	MaxOpenConns             int
	MaxIdleConns             int
}

func LoadConfigFromEnv() Config {
	return Config{
		ListenAddress:            envOrDefault("GO_CITY_LISTEN_ADDRESS", envOrDefault("HTTP_ADDR", ":5202")),
		PostgresConnectionString: firstEnv("POSTGRES_CONNECTION_STRING", "ConnectionStrings__Postgres", "ConnectionStrings__DefaultConnection"),
		DotnetUpstream:           firstEnv("GO_CITY_DOTNET_UPSTREAM"),
		QueryTimeout:             durationEnvOrDefault("GO_CITY_QUERY_TIMEOUT", 5*time.Second),
		MaxOpenConns:             intEnvOrDefault("GO_CITY_DB_MAX_OPEN_CONNS", 10),
		MaxIdleConns:             intEnvOrDefault("GO_CITY_DB_MAX_IDLE_CONNS", 5),
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
