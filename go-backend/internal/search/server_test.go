package search

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"os"
	"reflect"
	"testing"
	"time"
)

func TestSearchReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/search?query=nomad&type=all&page=1&pageSize=20", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "search_all_response.json"), recorder.Body.Bytes())
}

func TestSearchCitiesReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/search/cities?query=lisbon&page=1&pageSize=20", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "search_cities_response.json"), recorder.Body.Bytes())
}

func TestSearchCoworkingsReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/search/coworkings?query=hub&page=1&pageSize=20", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "search_coworkings_response.json"), recorder.Body.Bytes())
}

func TestSuggestReturnsGoldenResponse(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/search/suggest?prefix=li&type=all&size=10", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", recorder.Code, recorder.Body.String())
	}
	assertJSONEqual(t, readTestdata(t, "search_suggest_response.json"), recorder.Body.Bytes())
}

func TestSuggestRequiresPrefix(t *testing.T) {
	server := newTestServer()
	recorder := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/api/v1/search/suggest?prefix=", nil)

	server.ServeHTTP(recorder, request)

	if recorder.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d: %s", recorder.Code, recorder.Body.String())
	}
}

func newTestServer() *Server {
	client := &StubClient{
		Cities: SearchResult[CitySearchDocument]{
			Items: []SearchResultItem[CitySearchDocument]{
				{Document: CitySearchDocument{ID: "city-1", Name: "Lisbon", Country: "Portugal", IsActive: true, CreatedAt: "2026-05-07T10:00:00Z", DocumentType: "city", UserCount: 120, ModeratorCount: 1, CoworkingCount: 18, MeetupCount: 9, ReviewCount: 42, OverallScore: floatPtr(4.7)}, Score: floatPtr(9.8), Highlights: map[string][]string{"name": {"<em>Lisbon</em>"}}},
			},
			TotalCount: 1,
			Took:       12,
			Page:       1,
			PageSize:   20,
			TotalPages: 1,
			HasMore:    false,
		},
		Coworkings: SearchResult[CoworkingSearchDocument]{
			Items: []SearchResultItem[CoworkingSearchDocument]{
				{Document: CoworkingSearchDocument{ID: "cow-1", Name: "Nomad Hub Lisbon", Address: "Rua Augusta 10", Currency: "EUR", Rating: 4.8, ReviewCount: 27, IsActive: true, VerificationStatus: "verified", CreatedAt: "2026-05-07T10:00:00Z", UpdatedAt: "2026-05-07T10:00:00Z", DocumentType: "coworking", CityName: ptrString("Lisbon")}, Score: floatPtr(8.9), Highlights: map[string][]string{"name": {"Nomad <em>Hub</em> Lisbon"}}},
			},
			TotalCount: 1,
			Took:       15,
			Page:       1,
			PageSize:   20,
			TotalPages: 1,
			HasMore:    false,
		},
		CitySuggest: []SuggestItem{{Text: "Lisbon", ID: "city-1", Type: "city", Score: 9.8, Metadata: map[string]any{"country": "Portugal"}}},
		CowSuggest:  []SuggestItem{{Text: "Nomad Hub Lisbon", ID: "cow-1", Type: "coworking", Score: 8.9, Metadata: map[string]any{"cityName": "Lisbon"}}},
	}
	server := NewServer(Config{ListenAddress: ":5215"}, client, slog.Default())
	server.now = func() time.Time { return time.Date(2026, 5, 7, 10, 0, 0, 0, time.UTC) }
	return server
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

func floatPtr(value float64) *float64 { return &value }
