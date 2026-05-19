package ai

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"strings"
	"time"
)

type Server struct {
	config  Config
	logger  *slog.Logger
	sidecar *SidecarClient
	tasks   TaskStore
	events  EventPublisher
	now     func() time.Time
}

func NewServer(config Config, logger *slog.Logger) *Server {
	if logger == nil {
		logger = slog.Default()
	}

	localTasks := NewMemoryTaskStore(config.TaskTTL)
	tasks := TaskStore(localTasks)
	if config.RedisConnectionString != "" {
		redisTasks, err := NewRedisTaskStore(config.RedisConnectionString, config.TaskTTL, localTasks)
		if err != nil {
			logger.Warn("redis task store unavailable, using in-memory fallback", "error", err)
		} else {
			tasks = redisTasks
			logger.Info("redis task store enabled")
		}
	}

	events := EventPublisher(NoopEventPublisher{})
	if config.RabbitMQHost != "" {
		publisher, err := NewRabbitMQPublisher(config)
		if err != nil {
			logger.Warn("rabbitmq publisher unavailable, using no-op event publisher", "error", err)
		} else {
			events = publisher
			logger.Info("rabbitmq event publisher enabled", "host", config.RabbitMQHost)
		}
	}

	return &Server{
		config:  config,
		logger:  logger,
		sidecar: NewSidecarClient(config.SidecarURL, config.HTTPTimeout),
		tasks:   tasks,
		events:  events,
		now:     func() time.Time { return time.Now().UTC() },
	}
}

func (server *Server) ServeHTTP(response http.ResponseWriter, request *http.Request) {
	switch {
	case request.Method == http.MethodGet && request.URL.Path == "/health":
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "go-ai-image-service", "timestamp": server.now()})
	case request.Method == http.MethodPost && request.URL.Path == "/api/v1/ai/images/generate":
		server.handleGenerateImage(response, request)
	case request.Method == http.MethodPost && request.URL.Path == "/api/v1/ai/images/city":
		server.handleGenerateCityImages(response, request)
	case request.Method == http.MethodPost && request.URL.Path == "/api/v1/ai/images/city/async":
		server.handleGenerateCityImagesAsync(response, request)
	case request.Method == http.MethodGet && strings.HasPrefix(request.URL.Path, "/api/v1/ai/images/tasks/"):
		server.handleGetImageTaskStatus(response, request)
	default:
		writeJSON(response, http.StatusNotFound, APIResponse[map[string]any]{Success: false, Message: "No route matched"})
	}
}

func (server *Server) handleGenerateImage(response http.ResponseWriter, request *http.Request) {
	var payload GenerateImageRequest
	if !decodeJSON(response, request, &payload) {
		return
	}
	if err := validateImageRequest(payload); err != nil {
		writeJSON(response, http.StatusBadRequest, APIResponse[GenerateImageResponse]{Success: false, Message: err.Error()})
		return
	}

	result, err := server.sidecar.GenerateImage(request.Context(), normalizeImageRequest(payload))
	if err != nil {
		server.logger.Error("image sidecar generate failed", "error", err)
		writeJSON(response, http.StatusBadGateway, APIResponse[GenerateImageResponse]{Success: false, Message: "图片生成服务暂不可用"})
		return
	}
	if !result.Success {
		writeJSON(response, http.StatusBadRequest, APIResponse[GenerateImageResponse]{Success: false, Message: fallbackMessage(result.ErrorMessage, "图片生成失败"), Data: &result})
		return
	}

	writeJSON(response, http.StatusOK, APIResponse[GenerateImageResponse]{Success: true, Message: "成功生成图片", Data: &result})
}

func (server *Server) handleGenerateCityImages(response http.ResponseWriter, request *http.Request) {
	var payload GenerateCityImagesRequest
	if !decodeJSON(response, request, &payload) {
		return
	}
	if err := validateCityImagesRequest(payload); err != nil {
		writeJSON(response, http.StatusBadRequest, APIResponse[GenerateCityImagesResponse]{Success: false, Message: err.Error()})
		return
	}

	result, err := server.sidecar.GenerateCityImages(request.Context(), normalizeCityImagesRequest(payload, request.Header.Get("X-User-Id")))
	if err != nil {
		server.logger.Error("image sidecar city generate failed", "cityId", payload.CityID, "error", err)
		writeJSON(response, http.StatusBadGateway, APIResponse[GenerateCityImagesResponse]{Success: false, Message: "城市图片生成服务暂不可用"})
		return
	}
	if !result.Success {
		writeJSON(response, http.StatusBadRequest, APIResponse[GenerateCityImagesResponse]{Success: false, Message: fallbackMessage(result.ErrorMessage, "城市图片生成失败"), Data: &result})
		return
	}

	writeJSON(response, http.StatusOK, APIResponse[GenerateCityImagesResponse]{Success: true, Message: "成功生成城市图片", Data: &result})
}

func (server *Server) handleGenerateCityImagesAsync(response http.ResponseWriter, request *http.Request) {
	var payload GenerateCityImagesRequest
	if !decodeJSON(response, request, &payload) {
		return
	}
	if err := validateCityImagesRequest(payload); err != nil {
		writeJSON(response, http.StatusBadRequest, APIResponse[CreateTaskResponse]{Success: false, Message: err.Error()})
		return
	}

	taskID, err := newTaskID()
	if err != nil {
		writeJSON(response, http.StatusInternalServerError, APIResponse[CreateTaskResponse]{Success: false, Message: "创建任务失败"})
		return
	}

	now := server.now()
	server.tasks.Set(ImageTaskStatusResponse{
		TaskID:    taskID,
		Status:    "queued",
		Progress:  0,
		Message:   "图片生成任务已创建，正在处理...",
		CreatedAt: now,
		UpdatedAt: now,
	})

	requestUserID := request.Header.Get("X-User-Id")
	go server.runCityImageTask(taskID, normalizeCityImagesRequest(payload, requestUserID), now)

	result := CreateTaskResponse{
		TaskID:               taskID,
		Status:               "queued",
		EstimatedTimeSeconds: 180,
		Message:              "图片生成任务已创建，生成完成后将通过 SignalR 通知。",
	}
	writeJSON(response, http.StatusOK, APIResponse[CreateTaskResponse]{Success: true, Message: "图片生成任务已创建", Data: &result})
}

func (server *Server) handleGetImageTaskStatus(response http.ResponseWriter, request *http.Request) {
	taskID := strings.TrimPrefix(request.URL.Path, "/api/v1/ai/images/tasks/")
	if taskID == "" {
		writeJSON(response, http.StatusBadRequest, APIResponse[ImageTaskStatusResponse]{Success: false, Message: "taskId is required"})
		return
	}

	if status, ok := server.tasks.Get(taskID); ok {
		writeJSON(response, http.StatusOK, APIResponse[ImageTaskStatusResponse]{Success: true, Message: "任务状态: " + status.Status, Data: &status})
		return
	}

	status, err := server.sidecar.GetTaskStatus(request.Context(), taskID)
	if err != nil {
		writeJSON(response, http.StatusOK, APIResponse[ImageTaskStatusResponse]{Success: true, Message: "任务状态: UNKNOWN", Data: &ImageTaskStatusResponse{TaskID: taskID, Status: "UNKNOWN"}})
		return
	}

	writeJSON(response, http.StatusOK, APIResponse[ImageTaskStatusResponse]{Success: true, Message: "任务状态: " + status.Status, Data: &status})
}

func (server *Server) runCityImageTask(taskID string, payload GenerateCityImagesRequest, startTime time.Time) {
	processingTime := server.now()
	server.tasks.Set(ImageTaskStatusResponse{
		TaskID:    taskID,
		Status:    "processing",
		Progress:  10,
		Message:   "正在生成城市图片...",
		CreatedAt: startTime,
		UpdatedAt: processingTime,
	})
	if err := server.events.PublishAIProgress(context.Background(), AIProgressMessage{
		TaskID:       taskID,
		UserID:       payload.UserID,
		Progress:     10,
		Message:      "正在生成城市图片...",
		TaskType:     "city-image",
		CurrentStage: "generating",
		Status:       "processing",
		Timestamp:    processingTime,
	}); err != nil {
		server.logger.Warn("publish ai progress failed", "taskId", taskID, "error", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), server.config.HTTPTimeout)
	defer cancel()

	result, err := server.sidecar.GenerateCityImages(ctx, payload)
	completedAt := server.now()
	if err != nil {
		server.logger.Error("async city image task failed", "taskId", taskID, "cityId", payload.CityID, "error", err)
		server.tasks.Set(ImageTaskStatusResponse{
			TaskID:       taskID,
			Status:       "failed",
			Progress:     100,
			Message:      "城市图片生成失败",
			ErrorMessage: err.Error(),
			CreatedAt:    startTime,
			UpdatedAt:    completedAt,
			CompletedAt:  completedAt,
		})
		server.publishCityImageGenerated(taskID, payload, nil, false, err.Error(), startTime, completedAt)
		return
	}

	imageURLs := make([]string, 0, 1+len(result.LandscapeImages))
	if result.PortraitImage != nil {
		imageURLs = append(imageURLs, result.PortraitImage.URL)
	}
	for _, image := range result.LandscapeImages {
		imageURLs = append(imageURLs, image.URL)
	}

	status := "completed"
	message := "城市图片生成完成"
	failedCount := 0
	if !result.Success {
		status = "failed"
		message = fallbackMessage(result.ErrorMessage, "城市图片生成失败")
		failedCount = 1
	}

	server.tasks.Set(ImageTaskStatusResponse{
		TaskID:         taskID,
		Status:         status,
		ImageURLs:      imageURLs,
		SucceededCount: len(imageURLs),
		FailedCount:    failedCount,
		ErrorMessage:   result.ErrorMessage,
		Progress:       100,
		Message:        message,
		CreatedAt:      startTime,
		UpdatedAt:      completedAt,
		CompletedAt:    completedAt,
	})
	server.publishCityImageGenerated(taskID, payload, imageURLs, result.Success, result.ErrorMessage, startTime, completedAt)
}

func (server *Server) publishCityImageGenerated(taskID string, payload GenerateCityImagesRequest, imageURLs []string, success bool, errorMessage string, startTime time.Time, completedAt time.Time) {
	portraitURL := ""
	landscapeURLs := imageURLs
	if len(imageURLs) > 0 {
		portraitURL = imageURLs[0]
		landscapeURLs = imageURLs[1:]
	}
	if err := server.events.PublishCityImageGenerated(context.Background(), CityImageGeneratedMessage{
		TaskID:             taskID,
		CityID:             payload.CityID,
		CityName:           payload.CityName,
		UserID:             payload.UserID,
		PortraitImageURL:   portraitURL,
		LandscapeImageURLs: landscapeURLs,
		Success:            success,
		ErrorMessage:       errorMessage,
		CompletedAt:        completedAt,
		DurationSeconds:    int(completedAt.Sub(startTime).Seconds()),
	}); err != nil {
		server.logger.Warn("publish city image generated failed", "taskId", taskID, "error", err)
	}
}

func decodeJSON(response http.ResponseWriter, request *http.Request, target any) bool {
	defer request.Body.Close()
	decoder := json.NewDecoder(request.Body)
	if err := decoder.Decode(target); err != nil {
		writeJSON(response, http.StatusBadRequest, APIResponse[map[string]any]{Success: false, Message: "Invalid JSON body"})
		return false
	}
	return true
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json; charset=utf-8")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}

func normalizeImageRequest(request GenerateImageRequest) GenerateImageRequest {
	if request.Style == "" {
		request.Style = "<auto>"
	}
	if request.Size == "" {
		request.Size = "1024*1024"
	}
	if request.Count == 0 {
		request.Count = 1
	}
	if request.Bucket == "" {
		request.Bucket = "city-photos"
	}
	return request
}

func normalizeCityImagesRequest(request GenerateCityImagesRequest, headerUserID string) GenerateCityImagesRequest {
	if request.Style == "" {
		request.Style = "<photography>"
	}
	if request.Bucket == "" {
		request.Bucket = "city-photos"
	}
	if request.UserID == "" {
		request.UserID = headerUserID
	}
	if request.UserID == "" {
		request.UserID = "00000000-0000-0000-0000-000000000001"
	}
	return request
}

func validateImageRequest(request GenerateImageRequest) error {
	request = normalizeImageRequest(request)
	if strings.TrimSpace(request.Prompt) == "" {
		return errors.New("提示词不能为空")
	}
	if len([]rune(request.Prompt)) > 800 {
		return errors.New("提示词不能超过800个字符")
	}
	if len([]rune(request.NegativePrompt)) > 800 {
		return errors.New("反向提示词不能超过800个字符")
	}
	if request.Count < 1 || request.Count > 4 {
		return errors.New("生成数量必须在1-4之间")
	}
	if request.Size != "1024*1024" && request.Size != "720*1280" && request.Size != "1280*720" {
		return errors.New("图片尺寸不支持")
	}
	return nil
}

func validateCityImagesRequest(request GenerateCityImagesRequest) error {
	if strings.TrimSpace(request.CityID) == "" {
		return errors.New("城市ID不能为空")
	}
	if strings.TrimSpace(request.CityName) == "" {
		return errors.New("城市名称不能为空")
	}
	return nil
}

func fallbackMessage(value string, fallback string) string {
	if strings.TrimSpace(value) == "" {
		return fallback
	}
	return value
}

func newTaskID() (string, error) {
	bytes := make([]byte, 16)
	if _, err := rand.Read(bytes); err != nil {
		return "", err
	}
	return hex.EncodeToString(bytes), nil
}
