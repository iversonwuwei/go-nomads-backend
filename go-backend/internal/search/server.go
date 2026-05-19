package search

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strconv"
	"strings"
	"time"
)

type Server struct {
	config Config
	client Client
	logger *slog.Logger
	now    func() time.Time
}

func NewServer(config Config, client Client, logger *slog.Logger) *Server {
	if logger == nil {
		logger = slog.Default()
	}
	return &Server{config: config, client: client, logger: logger, now: func() time.Time { return time.Now().UTC() }}
}

func (server *Server) ServeHTTP(response http.ResponseWriter, request *http.Request) {
	switch {
	case request.Method == http.MethodGet && request.URL.Path == "/health":
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "go-search-service", "timestamp": server.now()})
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/search":
		server.handleSearch(response, request)
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/search/cities":
		server.handleSearchCities(response, request)
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/search/coworkings":
		server.handleSearchCoworkings(response, request)
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/search/suggest":
		server.handleSuggest(response, request)
	default:
		writeJSON(response, http.StatusNotFound, ApiResponse[map[string]any]{Success: false, Message: ptrString("No route matched")})
	}
}

func (server *Server) handleSearch(response http.ResponseWriter, request *http.Request) {
	searchRequest := parseSearchRequest(request)
	result := UnifiedSearchResult{}
	start := time.Now()
	searchType := strings.ToLower(strings.TrimSpace(searchRequest.Type))
	if searchType == "" || searchType == "all" || searchType == "city" {
		cities, err := server.client.SearchCities(request.Context(), searchRequest)
		if err != nil {
			server.logger.Error("search cities failed", "error", err)
			writeJSON(response, http.StatusInternalServerError, ApiResponse[UnifiedSearchResult]{Success: false, Message: ptrString("Search failed")})
			return
		}
		result.Cities = cities
	}
	if searchType == "" || searchType == "all" || searchType == "coworking" {
		coworkings, err := server.client.SearchCoworkings(request.Context(), searchRequest)
		if err != nil {
			server.logger.Error("search coworkings failed", "error", err)
			writeJSON(response, http.StatusInternalServerError, ApiResponse[UnifiedSearchResult]{Success: false, Message: ptrString("Search failed")})
			return
		}
		result.Coworkings = coworkings
	}
	result.TotalTook = time.Since(start).Milliseconds()
	result.TotalCount = result.Cities.TotalCount + result.Coworkings.TotalCount
	writeJSON(response, http.StatusOK, ApiResponse[UnifiedSearchResult]{Success: true, Data: &result})
}

func (server *Server) handleSearchCities(response http.ResponseWriter, request *http.Request) {
	searchRequest := parseSearchRequest(request)
	result, err := server.client.SearchCities(request.Context(), searchRequest)
	if err != nil {
		server.logger.Error("search cities failed", "error", err)
		writeJSON(response, http.StatusInternalServerError, ApiResponse[SearchResult[CitySearchDocument]]{Success: false, Message: ptrString("Search failed")})
		return
	}
	writeJSON(response, http.StatusOK, ApiResponse[SearchResult[CitySearchDocument]]{Success: true, Data: &result})
}

func (server *Server) handleSearchCoworkings(response http.ResponseWriter, request *http.Request) {
	searchRequest := parseSearchRequest(request)
	result, err := server.client.SearchCoworkings(request.Context(), searchRequest)
	if err != nil {
		server.logger.Error("search coworkings failed", "error", err)
		writeJSON(response, http.StatusInternalServerError, ApiResponse[SearchResult[CoworkingSearchDocument]]{Success: false, Message: ptrString("Search failed")})
		return
	}
	writeJSON(response, http.StatusOK, ApiResponse[SearchResult[CoworkingSearchDocument]]{Success: true, Data: &result})
}

func (server *Server) handleSuggest(response http.ResponseWriter, request *http.Request) {
	prefix := strings.TrimSpace(request.URL.Query().Get("prefix"))
	if prefix == "" {
		writeJSON(response, http.StatusBadRequest, ApiResponse[SuggestResponse]{Success: false, Message: ptrString("搜索前缀不能为空")})
		return
	}
	size := intQueryOrDefault(request.URL.Query(), "size", 10)
	if size < 1 {
		size = 10
	}
	typeValue := strings.TrimSpace(request.URL.Query().Get("type"))
	requestModel := SuggestRequest{Prefix: prefix, Type: typeValue, Size: size}
	searchType := strings.ToLower(typeValue)
	var citySuggestions []SuggestItem
	var coworkingSuggestions []SuggestItem
	var err error
	if searchType == "" || searchType == "all" || searchType == "city" {
		citySuggestions, err = server.client.SuggestCities(request.Context(), requestModel)
		if err != nil {
			writeJSON(response, http.StatusInternalServerError, ApiResponse[SuggestResponse]{Success: false, Message: ptrString("Suggest failed")})
			return
		}
	}
	if searchType == "" || searchType == "all" || searchType == "coworking" {
		coworkingSuggestions, err = server.client.SuggestCoworkings(request.Context(), requestModel)
		if err != nil {
			writeJSON(response, http.StatusInternalServerError, ApiResponse[SuggestResponse]{Success: false, Message: ptrString("Suggest failed")})
			return
		}
	}
	result := SuggestResponse{Suggestions: mergeSuggestions(size, citySuggestions, coworkingSuggestions)}
	writeJSON(response, http.StatusOK, ApiResponse[SuggestResponse]{Success: true, Data: &result})
}

func parseSearchRequest(request *http.Request) SearchRequest {
	queryValues := request.URL.Query()
	page := intQueryOrDefault(queryValues, "page", 1)
	if page < 1 {
		page = 1
	}
	pageSize := intQueryOrDefault(queryValues, "pageSize", 20)
	if pageSize < 1 {
		pageSize = 20
	}
	if pageSize > 100 {
		pageSize = 100
	}
	searchRequest := SearchRequest{
		Query:       strings.TrimSpace(queryValues.Get("query")),
		Page:        page,
		PageSize:    pageSize,
		Type:        strings.TrimSpace(queryValues.Get("type")),
		Country:     strings.TrimSpace(queryValues.Get("country")),
		CityID:      strings.TrimSpace(queryValues.Get("cityId")),
		SortBy:      strings.TrimSpace(queryValues.Get("sortBy")),
		SortOrder:   strings.TrimSpace(queryValues.Get("sortOrder")),
		EnableFuzzy: true,
	}
	if searchRequest.SortOrder == "" {
		searchRequest.SortOrder = "desc"
	}
	if minRatingText := strings.TrimSpace(queryValues.Get("minRating")); minRatingText != "" {
		if parsed, err := strconv.ParseFloat(minRatingText, 64); err == nil {
			searchRequest.MinRating = &parsed
		}
	}
	if latitudeText := strings.TrimSpace(queryValues.Get("lat")); latitudeText != "" {
		if parsed, err := strconv.ParseFloat(latitudeText, 64); err == nil {
			searchRequest.Latitude = &parsed
		}
	}
	if longitudeText := strings.TrimSpace(queryValues.Get("lon")); longitudeText != "" {
		if parsed, err := strconv.ParseFloat(longitudeText, 64); err == nil {
			searchRequest.Longitude = &parsed
		}
	}
	if radiusText := strings.TrimSpace(queryValues.Get("radiusKm")); radiusText != "" {
		if parsed, err := strconv.ParseFloat(radiusText, 64); err == nil {
			searchRequest.RadiusKm = &parsed
		}
	}
	return searchRequest
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}
