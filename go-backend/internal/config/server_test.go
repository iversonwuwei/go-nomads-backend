package config

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"os"
	"reflect"
	"testing"
	"time"
)

type fakeSnapshotRepository struct {
	snapshot publishedSnapshot
	err      error
}

func (repository fakeSnapshotRepository) GetPublished(ctx context.Context) (publishedSnapshot, error) {
	return repository.snapshot, repository.err
}

func TestGetConfigUsesRequestedLocale(t *testing.T) {
	publishedAt := time.Date(2026, 5, 7, 12, 0, 0, 0, time.UTC)
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{snapshot: publishedSnapshot{
		Version:        3,
		PublishedAt:    &publishedAt,
		StaticTexts:    []byte(`{"zh-CN":{"hello":"你好"},"en-US":{"hello":"Hello"}}`),
		OptionGroups:   []byte(`{"locale":[{"code":"en","label":"English","labelEn":"English"}]}`),
		SystemSettings: []byte(`{"app":{"theme":{"label":"Theme","valueType":"string","value":"light"}}}`),
	}}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config?locale=en-US", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	var payload APIResponse[AppConfig]
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload.Data == nil || payload.Data.StaticTexts["hello"] != "Hello" {
		t.Fatalf("expected en-US static text, got %+v", payload.Data)
	}
	if payload.Data.OptionGroups["locale"][0].Code != "en" {
		t.Fatalf("expected option groups to be preserved, got %+v", payload.Data.OptionGroups)
	}
}

func TestGetConfigFallsBackToZhCN(t *testing.T) {
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{snapshot: publishedSnapshot{
		Version:        1,
		StaticTexts:    []byte(`{"zh-CN":{"hello":"你好"}}`),
		OptionGroups:   []byte(`{}`),
		SystemSettings: []byte(`{}`),
	}}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config?locale=fr-FR", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", recorder.Code)
	}

	var payload APIResponse[AppConfig]
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload.Data == nil || payload.Data.StaticTexts["hello"] != "你好" {
		t.Fatalf("expected zh-CN fallback, got %+v", payload.Data)
	}
}

func TestGetVersionReturnsPublishedMetadata(t *testing.T) {
	publishedAt := time.Date(2026, 5, 7, 12, 0, 0, 0, time.UTC)
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{snapshot: publishedSnapshot{Version: 7, PublishedAt: &publishedAt}}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config/version", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", recorder.Code)
	}

	var payload APIResponse[AppConfigVersion]
	if err := json.Unmarshal(recorder.Body.Bytes(), &payload); err != nil {
		t.Fatal(err)
	}
	if payload.Data == nil || payload.Data.Version != 7 {
		t.Fatalf("expected version 7, got %+v", payload.Data)
	}
}

func TestGetConfigReturnsNotFoundWhenNoPublishedSnapshotExists(t *testing.T) {
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{err: sql.ErrNoRows}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", recorder.Code)
	}
}

func TestGetConfigReturnsServerErrorForRepositoryFailure(t *testing.T) {
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{err: errors.New("database down")}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", recorder.Code)
	}
}

func TestGoldenAppConfigResponseUsesRequestedLocale(t *testing.T) {
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{snapshot: loadGoldenSnapshot(t)}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config?locale=en-US", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "app_config_en_us_response.json"), recorder.Body.Bytes())
}

func TestGoldenAppConfigResponseFallsBackToZhCN(t *testing.T) {
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{snapshot: loadGoldenSnapshot(t)}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config?locale=fr-FR", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "app_config_fr_fallback_response.json"), recorder.Body.Bytes())
}

func TestGoldenAppConfigVersionResponse(t *testing.T) {
	server := NewServerWithRepository(Config{QueryTimeout: time.Second}, fakeSnapshotRepository{snapshot: loadGoldenSnapshot(t)}, slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/app/config/version", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "app_config_version_response.json"), recorder.Body.Bytes())
}

func loadGoldenSnapshot(t *testing.T) publishedSnapshot {
	t.Helper()

	var fixture struct {
		Version        int             `json:"version"`
		PublishedAt    time.Time       `json:"publishedAt"`
		StaticTexts    json.RawMessage `json:"staticTexts"`
		OptionGroups   json.RawMessage `json:"optionGroups"`
		SystemSettings json.RawMessage `json:"systemSettings"`
	}
	if err := json.Unmarshal(readTestdata(t, "published_snapshot.json"), &fixture); err != nil {
		t.Fatal(err)
	}

	return publishedSnapshot{
		Version:        fixture.Version,
		PublishedAt:    &fixture.PublishedAt,
		StaticTexts:    []byte(fixture.StaticTexts),
		OptionGroups:   []byte(fixture.OptionGroups),
		SystemSettings: []byte(fixture.SystemSettings),
	}
}

func readTestdata(t *testing.T, fileName string) []byte {
	t.Helper()

	content, err := os.ReadFile("testdata/" + fileName)
	if err != nil {
		t.Fatal(err)
	}
	return content
}

func assertJSONEqual(t *testing.T, expected []byte, actual []byte) {
	t.Helper()

	var expectedValue any
	if err := json.Unmarshal(expected, &expectedValue); err != nil {
		t.Fatal(err)
	}

	var actualValue any
	if err := json.Unmarshal(actual, &actualValue); err != nil {
		t.Fatal(err)
	}

	if !reflect.DeepEqual(expectedValue, actualValue) {
		t.Fatalf("json mismatch\nexpected: %s\nactual:   %s", expected, actual)
	}
}
