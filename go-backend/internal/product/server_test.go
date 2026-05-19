package product

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"os"
	"reflect"
	"testing"
)

func TestGetProductsReturnsGoldenResponse(t *testing.T) {
	server := NewServer(Config{ListenAddress: ":5002"}, NewMemoryRepository(), slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/products?page=1&pageSize=10", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "list_products_page1_response.json"), recorder.Body.Bytes())
}

func TestGetProductReturnsGoldenResponse(t *testing.T) {
	server := NewServer(Config{ListenAddress: ":5002"}, NewMemoryRepository(), slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/products/1", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "get_product_1_response.json"), recorder.Body.Bytes())
}

func TestGetProductsByUserReturnsGoldenResponse(t *testing.T) {
	server := NewServer(Config{ListenAddress: ":5002"}, NewMemoryRepository(), slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/products/user/1?page=1&pageSize=10", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "get_products_user_1_response.json"), recorder.Body.Bytes())
}

func TestGetProductReturnsNotFound(t *testing.T) {
	server := NewServer(Config{ListenAddress: ":5002"}, NewMemoryRepository(), slog.Default())

	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/products/404", nil)
	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d: %s", recorder.Code, recorder.Body.String())
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
