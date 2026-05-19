package product

import (
	"os"
	"strings"
)

type Config struct {
	ListenAddress string
}

func LoadConfigFromEnv() Config {
	return Config{
		ListenAddress: envOrDefault("GO_PRODUCT_LISTEN_ADDRESS", envOrDefault("HTTP_ADDR", ":5002")),
	}
}

func envOrDefault(key string, fallback string) string {
	if value := strings.TrimSpace(os.Getenv(key)); value != "" {
		return value
	}
	return fallback
}
