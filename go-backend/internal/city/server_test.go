package city

import (
	"context"
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

type stubRepository struct {
	entries []regionSource
	err     error
}

func (repository *stubRepository) ListRegionSources(_ context.Context) ([]regionSource, error) {
	if repository.err != nil {
		return nil, repository.err
	}
	return repository.entries, nil
}

func TestRegionTabsReturnsGoldenResponse(t *testing.T) {
	repository := &stubRepository{entries: []regionSource{
		{Region: strPtr("europe"), Continent: strPtr("Europe")},
		{Region: strPtr("asia"), Continent: strPtr("asia")},
		{Region: strPtr("north_america"), Continent: strPtr("north_america")},
		{Region: strPtr("unknown"), Continent: nil},
		{Region: strPtr("unknown-2"), Continent: nil},
	}}
	server := NewServer(Config{ListenAddress: ":5202", QueryTimeout: 2 * time.Second}, repository, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cities/region-tabs", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "region_tabs_response.json"), recorder.Body.Bytes())
}

func TestRegionTabsReturnsInternalErrorWhenRepositoryFails(t *testing.T) {
	repository := &stubRepository{err: errors.New("db failed")}
	server := NewServer(Config{ListenAddress: ":5202", QueryTimeout: 2 * time.Second}, repository, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cities/region-tabs", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d: %s", recorder.Code, recorder.Body.String())
	}
}

func TestUnownedCityPathReturnsNotFoundWithoutDotnetProxy(t *testing.T) {
	repository := &stubRepository{entries: []regionSource{}}
	server := NewServer(Config{ListenAddress: ":5202", QueryTimeout: 2 * time.Second}, repository, slog.Default())
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/cities/list", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", recorder.Code)
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

func strPtr(value string) *string { return &value }
