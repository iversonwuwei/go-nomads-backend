package search

import (
	"os"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	ListenAddress    string
	ElasticsearchURL string
	CityIndex        string
	CoworkingIndex   string
	RequestTimeout   time.Duration
}

func LoadConfigFromEnv() Config {
	return Config{
		ListenAddress:    envOrDefault("GO_SEARCH_LISTEN_ADDRESS", envOrDefault("HTTP_ADDR", ":5215")),
		ElasticsearchURL: firstEnv("Elasticsearch__Url", "ELASTICSEARCH_URL"),
		CityIndex:        envOrDefault("IndexSettings__CityIndex", "cities"),
		CoworkingIndex:   envOrDefault("IndexSettings__CoworkingIndex", "coworking_spaces"),
		RequestTimeout:   durationEnvOrDefault("GO_SEARCH_REQUEST_TIMEOUT", 5*time.Second),
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

func intQueryOrDefault(values map[string][]string, key string, fallback int) int {
	entries := values[key]
	if len(entries) == 0 {
		return fallback
	}
	parsed, err := strconv.Atoi(strings.TrimSpace(entries[0]))
	if err != nil {
		return fallback
	}
	return parsed
}
