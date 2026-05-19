package ai

import "time"

type APIResponse[T any] struct {
	Success bool     `json:"success"`
	Message string   `json:"message"`
	Data    *T       `json:"data,omitempty"`
	Errors  []string `json:"errors,omitempty"`
}

type GenerateImageRequest struct {
	Prompt         string `json:"prompt"`
	NegativePrompt string `json:"negativePrompt,omitempty"`
	Style          string `json:"style,omitempty"`
	Size           string `json:"size,omitempty"`
	Count          int    `json:"count,omitempty"`
	Bucket         string `json:"bucket,omitempty"`
	PathPrefix     string `json:"pathPrefix,omitempty"`
}

type GenerateCityImagesRequest struct {
	CityID          string `json:"cityId"`
	CityName        string `json:"cityName"`
	Country         string `json:"country,omitempty"`
	PortraitPrompt  string `json:"portraitPrompt,omitempty"`
	LandscapePrompt string `json:"landscapePrompt,omitempty"`
	NegativePrompt  string `json:"negativePrompt,omitempty"`
	Style           string `json:"style,omitempty"`
	Bucket          string `json:"bucket,omitempty"`
	UserID          string `json:"userId,omitempty"`
}

type GeneratedImageInfo struct {
	URL         string `json:"url"`
	StoragePath string `json:"storagePath"`
	OriginalURL string `json:"originalUrl"`
	FileSize    int64  `json:"fileSize"`
}

type GenerateImageResponse struct {
	Images           []GeneratedImageInfo `json:"images"`
	TaskID           string               `json:"taskId"`
	GenerationTimeMs int                  `json:"generationTimeMs"`
	Success          bool                 `json:"success"`
	ErrorMessage     string               `json:"errorMessage,omitempty"`
}

type GenerateCityImagesResponse struct {
	CityID           string               `json:"cityId"`
	PortraitImage    *GeneratedImageInfo  `json:"portraitImage"`
	LandscapeImages  []GeneratedImageInfo `json:"landscapeImages"`
	GenerationTimeMs int                  `json:"generationTimeMs"`
	Success          bool                 `json:"success"`
	ErrorMessage     string               `json:"errorMessage,omitempty"`
}

type CreateTaskResponse struct {
	TaskID               string `json:"taskId"`
	Status               string `json:"status"`
	EstimatedTimeSeconds int    `json:"estimatedTimeSeconds"`
	Message              string `json:"message"`
}

type ImageTaskStatusResponse struct {
	TaskID         string    `json:"taskId"`
	Status         string    `json:"status"`
	ImageURLs      []string  `json:"imageUrls"`
	SucceededCount int       `json:"succeededCount"`
	FailedCount    int       `json:"failedCount"`
	ErrorMessage   string    `json:"errorMessage,omitempty"`
	Progress       int       `json:"progress,omitempty"`
	Message        string    `json:"message,omitempty"`
	CreatedAt      time.Time `json:"createdAt,omitempty"`
	UpdatedAt      time.Time `json:"updatedAt,omitempty"`
	CompletedAt    time.Time `json:"completedAt,omitempty"`
}
