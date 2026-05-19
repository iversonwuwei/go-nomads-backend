package ai

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strings"
	"time"
)

type SidecarClient struct {
	baseURL    string
	httpClient *http.Client
}

func NewSidecarClient(baseURL string, timeout time.Duration) *SidecarClient {
	return &SidecarClient{
		baseURL: strings.TrimRight(baseURL, "/"),
		httpClient: &http.Client{
			Timeout: timeout,
		},
	}
}

func (client *SidecarClient) GenerateImage(ctx context.Context, request GenerateImageRequest) (GenerateImageResponse, error) {
	var response GenerateImageResponse
	err := client.postJSON(ctx, "/internal/v1/images/generate", request, &response)
	return response, err
}

func (client *SidecarClient) GenerateCityImages(ctx context.Context, request GenerateCityImagesRequest) (GenerateCityImagesResponse, error) {
	var response GenerateCityImagesResponse
	err := client.postJSON(ctx, "/internal/v1/images/city", request, &response)
	return response, err
}

func (client *SidecarClient) GetTaskStatus(ctx context.Context, taskID string) (ImageTaskStatusResponse, error) {
	var response ImageTaskStatusResponse
	err := client.getJSON(ctx, "/internal/v1/images/tasks/"+taskID, &response)
	return response, err
}

func (client *SidecarClient) postJSON(ctx context.Context, path string, payload any, target any) error {
	body, err := json.Marshal(payload)
	if err != nil {
		return err
	}

	request, err := http.NewRequestWithContext(ctx, http.MethodPost, client.baseURL+path, bytes.NewReader(body))
	if err != nil {
		return err
	}
	request.Header.Set("Content-Type", "application/json")

	return client.do(request, target)
}

func (client *SidecarClient) getJSON(ctx context.Context, path string, target any) error {
	request, err := http.NewRequestWithContext(ctx, http.MethodGet, client.baseURL+path, nil)
	if err != nil {
		return err
	}

	return client.do(request, target)
}

func (client *SidecarClient) do(request *http.Request, target any) error {
	response, err := client.httpClient.Do(request)
	if err != nil {
		return err
	}
	defer response.Body.Close()

	body, err := io.ReadAll(response.Body)
	if err != nil {
		return err
	}

	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return fmt.Errorf("sidecar returned %d: %s", response.StatusCode, strings.TrimSpace(string(body)))
	}

	if len(body) == 0 {
		return nil
	}

	return json.Unmarshal(body, target)
}
