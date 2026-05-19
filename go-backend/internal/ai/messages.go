package ai

import (
	"encoding/json"
	"time"
)

type AIProgressMessage struct {
	TaskID       string    `json:"TaskId"`
	UserID       string    `json:"UserId"`
	Progress     int       `json:"Progress"`
	Message      string    `json:"Message"`
	TaskType     string    `json:"TaskType"`
	CurrentStage string    `json:"CurrentStage,omitempty"`
	Status       string    `json:"Status"`
	Timestamp    time.Time `json:"Timestamp"`
}

type CityImageGeneratedMessage struct {
	TaskID             string    `json:"TaskId"`
	CityID             string    `json:"CityId"`
	CityName           string    `json:"CityName"`
	UserID             string    `json:"UserId"`
	PortraitImageURL   string    `json:"PortraitImageUrl,omitempty"`
	LandscapeImageURLs []string  `json:"LandscapeImageUrls,omitempty"`
	Success            bool      `json:"Success"`
	ErrorMessage       string    `json:"ErrorMessage,omitempty"`
	CompletedAt        time.Time `json:"CompletedAt"`
	DurationSeconds    int       `json:"DurationSeconds"`
}

type massTransitEnvelope struct {
	MessageID     string          `json:"messageId"`
	SourceAddress string          `json:"sourceAddress,omitempty"`
	MessageType   []string        `json:"messageType"`
	Message       any             `json:"message"`
	SentTime      time.Time       `json:"sentTime"`
	Headers       json.RawMessage `json:"headers"`
	Host          envelopeHost    `json:"host"`
}

type envelopeHost struct {
	MachineName        string `json:"machineName"`
	ProcessName        string `json:"processName"`
	Assembly           string `json:"assembly"`
	AssemblyVersion    string `json:"assemblyVersion"`
	FrameworkVersion   string `json:"frameworkVersion"`
	MassTransitVersion string `json:"massTransitVersion"`
	OperatingSystem    string `json:"operatingSystem"`
}

func marshalMassTransitEnvelope(source string, messageTypes []string, message any) ([]byte, error) {
	return json.Marshal(massTransitEnvelope{
		MessageID:     newMessageID(),
		SourceAddress: source,
		MessageType:   messageTypes,
		Message:       message,
		SentTime:      time.Now().UTC(),
		Headers:       json.RawMessage(`{}`),
		Host: envelopeHost{
			MachineName:        "go-ai-service",
			ProcessName:        "go-ai-service",
			Assembly:           "go-nomads-go-ai-service",
			AssemblyVersion:    "0.1.0",
			FrameworkVersion:   "go",
			MassTransitVersion: "go-compatible-envelope",
			OperatingSystem:    "linux",
		},
	})
}
