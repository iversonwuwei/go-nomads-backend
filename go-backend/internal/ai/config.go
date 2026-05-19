package ai

import (
	"os"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	ListenAddress         string
	SidecarURL            string
	HTTPTimeout           time.Duration
	TaskTTL               time.Duration
	RedisConnectionString string
	RabbitMQHost          string
	RabbitMQPort          int
	RabbitMQUsername      string
	RabbitMQPassword      string
}

func LoadConfigFromEnv() Config {
	config := DefaultConfig()
	config.ListenAddress = envOrDefault("GO_AI_LISTEN_ADDRESS", envOrDefault("HTTP_ADDR", ":5221"))
	config.SidecarURL = envOrDefault("IMAGE_SIDECAR_URL", config.SidecarURL)
	config.RedisConnectionString = firstEnv("Redis__ConnectionString", "ConnectionStrings__Redis", "REDIS_CONNECTION_STRING")
	config.RabbitMQHost = firstEnv("RabbitMQ__HostName", "RabbitMQ__Host", "RABBITMQ_HOST")
	config.RabbitMQPort = intEnvOrDefault(firstEnv("RabbitMQ__Port", "RABBITMQ_PORT"), config.RabbitMQPort)
	config.RabbitMQUsername = firstEnv("RabbitMQ__UserName", "RabbitMQ__Username", "RABBITMQ_USERNAME")
	config.RabbitMQPassword = firstEnv("RabbitMQ__Password", "RABBITMQ_PASSWORD")
	return config
}

func DefaultConfig() Config {
	return Config{
		ListenAddress:    ":5221",
		SidecarURL:       "http://image-generation-sidecar:5222",
		HTTPTimeout:      5 * time.Minute,
		TaskTTL:          24 * time.Hour,
		RabbitMQPort:     5672,
		RabbitMQUsername: "walden",
		RabbitMQPassword: "walden",
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

func intEnvOrDefault(value string, fallback int) int {
	if strings.TrimSpace(value) == "" {
		return fallback
	}
	parsed, err := strconv.Atoi(value)
	if err != nil {
		return fallback
	}
	return parsed
}
