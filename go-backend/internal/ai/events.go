package ai

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"fmt"
	"net/url"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

const (
	aiProgressExchange         = "Shared.Messages:AIProgressMessage"
	cityImageGeneratedExchange = "Shared.Messages:CityImageGeneratedMessage"
	contentTypeMassTransitJSON = "application/vnd.masstransit+json"
)

type EventPublisher interface {
	PublishAIProgress(ctx context.Context, message AIProgressMessage) error
	PublishCityImageGenerated(ctx context.Context, message CityImageGeneratedMessage) error
}

type NoopEventPublisher struct{}

func (publisher NoopEventPublisher) PublishAIProgress(context.Context, AIProgressMessage) error {
	return nil
}

func (publisher NoopEventPublisher) PublishCityImageGenerated(context.Context, CityImageGeneratedMessage) error {
	return nil
}

type RabbitMQPublisher struct {
	connection *amqp.Connection
	channel    *amqp.Channel
	source     string
}

func (publisher *RabbitMQPublisher) Close() error {
	if publisher == nil {
		return nil
	}
	var closeErr error
	if publisher.channel != nil {
		closeErr = publisher.channel.Close()
	}
	if publisher.connection != nil {
		if err := publisher.connection.Close(); closeErr == nil {
			closeErr = err
		}
	}
	return closeErr
}

func NewRabbitMQPublisher(config Config) (*RabbitMQPublisher, error) {
	uri := rabbitMQURI(config)
	connection, err := amqp.Dial(uri)
	if err != nil {
		return nil, err
	}
	channel, err := connection.Channel()
	if err != nil {
		_ = connection.Close()
		return nil, err
	}
	for _, exchange := range []string{aiProgressExchange, cityImageGeneratedExchange} {
		if err := channel.ExchangeDeclare(exchange, "fanout", true, false, false, false, nil); err != nil {
			_ = channel.Close()
			_ = connection.Close()
			return nil, err
		}
	}
	return &RabbitMQPublisher{connection: connection, channel: channel, source: "rabbitmq://go-ai-service"}, nil
}

func (publisher *RabbitMQPublisher) PublishAIProgress(ctx context.Context, message AIProgressMessage) error {
	return publisher.publish(ctx, aiProgressExchange, []string{"urn:message:Shared.Messages:AIProgressMessage"}, message)
}

func (publisher *RabbitMQPublisher) PublishCityImageGenerated(ctx context.Context, message CityImageGeneratedMessage) error {
	return publisher.publish(ctx, cityImageGeneratedExchange, []string{"urn:message:Shared.Messages:CityImageGeneratedMessage"}, message)
}

func (publisher *RabbitMQPublisher) publish(ctx context.Context, exchange string, messageTypes []string, message any) error {
	body, err := marshalMassTransitEnvelope(publisher.source, messageTypes, message)
	if err != nil {
		return err
	}
	return publisher.channel.PublishWithContext(ctx, exchange, "", false, false, amqp.Publishing{
		ContentType:  contentTypeMassTransitJSON,
		DeliveryMode: amqp.Persistent,
		Timestamp:    time.Now().UTC(),
		Body:         body,
	})
}

func rabbitMQURI(config Config) string {
	if config.RabbitMQHost == "" {
		return ""
	}
	if parsed, err := url.Parse(config.RabbitMQHost); err == nil && parsed.Scheme != "" {
		return config.RabbitMQHost
	}
	username := url.QueryEscape(config.RabbitMQUsername)
	password := url.QueryEscape(config.RabbitMQPassword)
	return fmt.Sprintf("amqp://%s:%s@%s:%d/", username, password, config.RabbitMQHost, config.RabbitMQPort)
}

func newMessageID() string {
	bytes := make([]byte, 16)
	if _, err := rand.Read(bytes); err != nil {
		return fmt.Sprintf("%d", time.Now().UnixNano())
	}
	return hex.EncodeToString(bytes)
}
