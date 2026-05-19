package ai

import (
	"context"
	"encoding/json"
	"os"
	"testing"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

func TestRabbitMQPublisherPublishesMassTransitEnvelope(t *testing.T) {
	if os.Getenv("GO_NOMADS_RABBITMQ_INTEGRATION") != "1" {
		t.Skip("set GO_NOMADS_RABBITMQ_INTEGRATION=1 to run RabbitMQ smoke test")
	}

	config := rabbitMQIntegrationConfig()
	publisher, err := newRabbitMQPublisherWithRetry(config, 20*time.Second)
	if err != nil {
		t.Fatalf("connect publisher: %v", err)
	}
	defer func() { _ = publisher.Close() }()

	channel, err := publisher.connection.Channel()
	if err != nil {
		t.Fatalf("open smoke channel: %v", err)
	}
	defer func() { _ = channel.Close() }()

	progressQueue := declareBoundSmokeQueue(t, channel, aiProgressExchange)
	cityQueue := declareBoundSmokeQueue(t, channel, cityImageGeneratedExchange)

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	if err := publisher.PublishAIProgress(ctx, AIProgressMessage{
		TaskID:       "smoke-task-progress",
		UserID:       "smoke-user",
		Progress:     10,
		Message:      "smoke progress",
		TaskType:     "city-image",
		CurrentStage: "generating",
		Status:       "processing",
		Timestamp:    time.Date(2026, 5, 7, 0, 0, 0, 0, time.UTC),
	}); err != nil {
		t.Fatalf("publish progress: %v", err)
	}

	progressDelivery := waitForDelivery(t, channel, progressQueue)
	assertMassTransitDelivery(t, progressDelivery, "urn:message:Shared.Messages:AIProgressMessage", "TaskId", "smoke-task-progress")

	if err := publisher.PublishCityImageGenerated(ctx, CityImageGeneratedMessage{
		TaskID:             "smoke-task-city",
		CityID:             "00000000-0000-0000-0000-000000000001",
		CityName:           "Bangkok",
		UserID:             "smoke-user",
		PortraitImageURL:   "https://storage.local/portrait.png",
		LandscapeImageURLs: []string{"https://storage.local/landscape-1.png"},
		Success:            true,
		CompletedAt:        time.Date(2026, 5, 7, 0, 1, 0, 0, time.UTC),
		DurationSeconds:    60,
	}); err != nil {
		t.Fatalf("publish city image generated: %v", err)
	}

	cityDelivery := waitForDelivery(t, channel, cityQueue)
	assertMassTransitDelivery(t, cityDelivery, "urn:message:Shared.Messages:CityImageGeneratedMessage", "CityId", "00000000-0000-0000-0000-000000000001")
}

func newRabbitMQPublisherWithRetry(config Config, timeout time.Duration) (*RabbitMQPublisher, error) {
	deadline := time.Now().Add(timeout)
	var lastErr error
	for time.Now().Before(deadline) {
		publisher, err := NewRabbitMQPublisher(config)
		if err == nil {
			return publisher, nil
		}
		lastErr = err
		time.Sleep(500 * time.Millisecond)
	}
	return nil, lastErr
}

func rabbitMQIntegrationConfig() Config {
	config := DefaultConfig()
	if value := firstEnv("RabbitMQ__HostName", "RabbitMQ__Host", "RABBITMQ_HOST"); value != "" {
		config.RabbitMQHost = value
	} else {
		config.RabbitMQHost = "localhost"
	}
	config.RabbitMQPort = intEnvOrDefault(firstEnv("RabbitMQ__Port", "RABBITMQ_PORT"), config.RabbitMQPort)
	if value := firstEnv("RabbitMQ__UserName", "RabbitMQ__Username", "RABBITMQ_USERNAME"); value != "" {
		config.RabbitMQUsername = value
	}
	if value := firstEnv("RabbitMQ__Password", "RABBITMQ_PASSWORD"); value != "" {
		config.RabbitMQPassword = value
	}
	return config
}

func declareBoundSmokeQueue(t *testing.T, channel *amqp.Channel, exchange string) string {
	t.Helper()
	queue, err := channel.QueueDeclare("", false, true, true, false, nil)
	if err != nil {
		t.Fatalf("declare smoke queue: %v", err)
	}
	if err := channel.QueueBind(queue.Name, "", exchange, false, nil); err != nil {
		t.Fatalf("bind smoke queue to %s: %v", exchange, err)
	}
	return queue.Name
}

func waitForDelivery(t *testing.T, channel *amqp.Channel, queueName string) amqp.Delivery {
	t.Helper()
	deliveries, err := channel.Consume(queueName, "", true, true, false, false, nil)
	if err != nil {
		t.Fatalf("consume smoke queue: %v", err)
	}
	select {
	case delivery := <-deliveries:
		return delivery
	case <-time.After(5 * time.Second):
		t.Fatalf("timed out waiting for delivery on %s", queueName)
	}
	return amqp.Delivery{}
}

func assertMassTransitDelivery(t *testing.T, delivery amqp.Delivery, expectedMessageType string, payloadKey string, payloadValue string) {
	t.Helper()
	if delivery.ContentType != contentTypeMassTransitJSON {
		t.Fatalf("unexpected content type %q", delivery.ContentType)
	}

	var envelope map[string]any
	if err := json.Unmarshal(delivery.Body, &envelope); err != nil {
		t.Fatalf("unmarshal envelope: %v", err)
	}
	messageTypes, ok := envelope["messageType"].([]any)
	if !ok || len(messageTypes) == 0 || messageTypes[0] != expectedMessageType {
		t.Fatalf("unexpected message types: %+v", envelope["messageType"])
	}
	message, ok := envelope["message"].(map[string]any)
	if !ok || message[payloadKey] != payloadValue {
		t.Fatalf("unexpected message payload: %+v", envelope["message"])
	}
}
