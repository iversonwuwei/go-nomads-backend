package ai

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"
)

func TestGenerateImageWrapsSidecarResponse(t *testing.T) {
	sidecar := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		if request.URL.Path != "/internal/v1/images/generate" {
			t.Fatalf("unexpected sidecar path %s", request.URL.Path)
		}
		_ = json.NewEncoder(response).Encode(GenerateImageResponse{
			Images:  []GeneratedImageInfo{{URL: "https://storage.local/city-photos/generated/image.png", StoragePath: "generated/image.png"}},
			TaskID:  "wanx-task-1",
			Success: true,
		})
	}))
	defer sidecar.Close()

	server := newTestServer(sidecar.URL)
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/ai/images/generate", strings.NewReader(`{"prompt":"Bangkok skyline","count":1}`))

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload APIResponse[GenerateImageResponse]
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if !payload.Success || payload.Data == nil || payload.Data.TaskID != "wanx-task-1" {
		t.Fatalf("unexpected response: %+v", payload)
	}
}

func TestGenerateCityImagesAsyncCompletesInMemoryTask(t *testing.T) {
	sidecar := httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		if request.URL.Path != "/internal/v1/images/city" {
			t.Fatalf("unexpected sidecar path %s", request.URL.Path)
		}
		_ = json.NewEncoder(response).Encode(GenerateCityImagesResponse{
			CityID:        "city-1",
			PortraitImage: &GeneratedImageInfo{URL: "https://storage.local/city-photos/portrait/city-1/image.png"},
			LandscapeImages: []GeneratedImageInfo{
				{URL: "https://storage.local/city-photos/landscape/city-1/1.png"},
				{URL: "https://storage.local/city-photos/landscape/city-1/2.png"},
			},
			Success: true,
		})
	}))
	defer sidecar.Close()

	server := newTestServer(sidecar.URL)
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/ai/images/city/async", strings.NewReader(`{"cityId":"city-1","cityName":"Bangkok"}`))
	request.Header.Set("X-User-Id", "user-1")

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var created APIResponse[CreateTaskResponse]
	if err := json.Unmarshal(recorder.Body.Bytes(), &created); err != nil {
		t.Fatal(err)
	}
	if created.Data == nil || created.Data.TaskID == "" {
		t.Fatalf("missing task id: %+v", created)
	}

	var status ImageTaskStatusResponse
	deadline := time.Now().Add(2 * time.Second)
	for time.Now().Before(deadline) {
		statusRecorder := httptest.NewRecorder()
		statusRequest := httptest.NewRequest(http.MethodGet, "/api/v1/ai/images/tasks/"+created.Data.TaskID, nil)
		server.ServeHTTP(statusRecorder, statusRequest)

		var statusPayload APIResponse[ImageTaskStatusResponse]
		if err := json.Unmarshal(statusRecorder.Body.Bytes(), &statusPayload); err != nil {
			t.Fatal(err)
		}
		if statusPayload.Data != nil {
			status = *statusPayload.Data
		}
		if status.Status == "completed" {
			break
		}
		time.Sleep(10 * time.Millisecond)
	}

	if status.Status != "completed" {
		t.Fatalf("expected completed task, got %+v", status)
	}
	if status.SucceededCount != 3 {
		t.Fatalf("expected 3 generated urls, got %+v", status)
	}
}

func TestRejectsInvalidImageRequest(t *testing.T) {
	server := newTestServer("http://127.0.0.1:1")
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/api/v1/ai/images/generate", strings.NewReader(`{"prompt":"","count":5}`))

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", recorder.Code)
	}
}

func newTestServer(sidecarURL string) *Server {
	config := DefaultConfig()
	config.SidecarURL = sidecarURL
	config.HTTPTimeout = 2 * time.Second
	server := NewServer(config, slog.Default())
	return server
}
