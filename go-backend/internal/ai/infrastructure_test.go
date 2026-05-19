package ai

import (
	"encoding/json"
	"testing"
	"time"
)

func TestRedisOptionsSupportsDotNetConnectionString(t *testing.T) {
	options, err := redisOptions("go-nomads-redis:6379,abortConnect=false")
	if err != nil {
		t.Fatal(err)
	}
	if options.Addr != "go-nomads-redis:6379" {
		t.Fatalf("unexpected redis address %s", options.Addr)
	}
}

func TestRabbitMQURISupportsHostAndFullURI(t *testing.T) {
	config := DefaultConfig()
	config.RabbitMQHost = "go-nomads-rabbitmq"
	config.RabbitMQPort = 5672
	config.RabbitMQUsername = "walden"
	config.RabbitMQPassword = "walden"

	if got := rabbitMQURI(config); got != "amqp://walden:walden@go-nomads-rabbitmq:5672/" {
		t.Fatalf("unexpected rabbit uri %s", got)
	}

	config.RabbitMQHost = "amqp://user:pass@rabbit:5672/"
	if got := rabbitMQURI(config); got != config.RabbitMQHost {
		t.Fatalf("expected full uri passthrough, got %s", got)
	}
}

func TestMassTransitEnvelopeContainsMessageTypeAndPayload(t *testing.T) {
	body, err := marshalMassTransitEnvelope("rabbitmq://go-ai-service", []string{"urn:message:Shared.Messages:AIProgressMessage"}, AIProgressMessage{
		TaskID:    "task-1",
		UserID:    "user-1",
		Progress:  10,
		Message:   "正在生成城市图片...",
		TaskType:  "city-image",
		Status:    "processing",
		Timestamp: time.Date(2026, 5, 7, 0, 0, 0, 0, time.UTC),
	})
	if err != nil {
		t.Fatal(err)
	}

	var envelope map[string]any
	if err := json.Unmarshal(body, &envelope); err != nil {
		t.Fatal(err)
	}
	messageTypes, ok := envelope["messageType"].([]any)
	if !ok || len(messageTypes) != 1 || messageTypes[0] != "urn:message:Shared.Messages:AIProgressMessage" {
		t.Fatalf("unexpected message types: %+v", envelope["messageType"])
	}
	message, ok := envelope["message"].(map[string]any)
	if !ok || message["TaskId"] != "task-1" || message["TaskType"] != "city-image" {
		t.Fatalf("unexpected message payload: %+v", envelope["message"])
	}
}
