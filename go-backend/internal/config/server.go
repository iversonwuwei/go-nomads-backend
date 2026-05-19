package config

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"strings"
	"time"
)

type Server struct {
	config     Config
	logger     *slog.Logger
	repository SnapshotRepository
	now        func() time.Time
}

func NewServer(config Config, database *sql.DB, logger *slog.Logger) *Server {
	return NewServerWithRepository(config, NewPostgresSnapshotRepository(database), logger)
}

func NewServerWithRepository(config Config, repository SnapshotRepository, logger *slog.Logger) *Server {
	if logger == nil {
		logger = slog.Default()
	}
	return &Server{
		config:     config,
		logger:     logger,
		repository: repository,
		now:        func() time.Time { return time.Now().UTC() },
	}
}

func (server *Server) ServeHTTP(response http.ResponseWriter, request *http.Request) {
	switch {
	case request.Method == http.MethodGet && request.URL.Path == "/health":
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "go-config-service", "timestamp": server.now()})
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/app/config":
		server.handleGetConfig(response, request)
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/app/config/version":
		server.handleGetVersion(response, request)
	default:
		writeJSON(response, http.StatusNotFound, APIResponse[map[string]any]{Success: false, Message: "No route matched"})
	}
}

func (server *Server) handleGetConfig(response http.ResponseWriter, request *http.Request) {
	snapshot, ok := server.loadPublishedSnapshot(response, request)
	if !ok {
		return
	}

	config, err := mapSnapshotToConfig(snapshot, request.URL.Query().Get("locale"))
	if err != nil {
		server.logger.Error("map app config snapshot failed", "error", err)
		writeJSON(response, http.StatusInternalServerError, APIResponse[AppConfig]{Success: false, Message: "获取配置失败"})
		return
	}

	writeJSON(response, http.StatusOK, APIResponse[AppConfig]{Success: true, Message: "Success", Data: &config})
}

func (server *Server) handleGetVersion(response http.ResponseWriter, request *http.Request) {
	snapshot, ok := server.loadPublishedSnapshot(response, request)
	if !ok {
		return
	}

	version := AppConfigVersion{Version: snapshot.Version, PublishedAt: snapshot.PublishedAt}
	writeJSON(response, http.StatusOK, APIResponse[AppConfigVersion]{Success: true, Message: "Success", Data: &version})
}

func (server *Server) loadPublishedSnapshot(response http.ResponseWriter, request *http.Request) (publishedSnapshot, bool) {
	ctx, cancel := context.WithTimeout(request.Context(), server.config.QueryTimeout)
	defer cancel()

	snapshot, err := server.repository.GetPublished(ctx)
	if errors.Is(err, sql.ErrNoRows) {
		writeJSON(response, http.StatusNotFound, APIResponse[map[string]any]{Success: false, Message: "暂无已发布的配置"})
		return publishedSnapshot{}, false
	}
	if err != nil {
		server.logger.Error("load published app config failed", "error", err)
		writeJSON(response, http.StatusInternalServerError, APIResponse[map[string]any]{Success: false, Message: "获取配置失败"})
		return publishedSnapshot{}, false
	}

	return snapshot, true
}

func mapSnapshotToConfig(snapshot publishedSnapshot, locale string) (AppConfig, error) {
	staticTextsByLocale := map[string]map[string]string{}
	if err := json.Unmarshal(defaultJSON(snapshot.StaticTexts), &staticTextsByLocale); err != nil {
		return AppConfig{}, err
	}

	optionGroups := map[string][]AppOptionItem{}
	if err := json.Unmarshal(defaultJSON(snapshot.OptionGroups), &optionGroups); err != nil {
		return AppConfig{}, err
	}

	systemSettings := map[string]map[string]SystemValue{}
	if err := json.Unmarshal(defaultJSON(snapshot.SystemSettings), &systemSettings); err != nil {
		return AppConfig{}, err
	}

	effectiveLocale := strings.TrimSpace(locale)
	if effectiveLocale == "" {
		effectiveLocale = "zh-CN"
	}

	staticTexts := staticTextsByLocale[effectiveLocale]
	if staticTexts == nil {
		staticTexts = staticTextsByLocale["zh-CN"]
	}
	if staticTexts == nil {
		staticTexts = map[string]string{}
	}

	return AppConfig{
		Version:        snapshot.Version,
		PublishedAt:    snapshot.PublishedAt,
		StaticTexts:    staticTexts,
		OptionGroups:   optionGroups,
		SystemSettings: systemSettings,
	}, nil
}

func defaultJSON(value []byte) []byte {
	if len(value) == 0 || strings.TrimSpace(string(value)) == "" {
		return []byte("{}")
	}
	return value
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}
