package city

import (
	"context"
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httputil"
	"net/url"
	"sort"
	"strings"
	"time"
)

type Server struct {
	config     Config
	repository Repository
	logger     *slog.Logger
	now        func() time.Time
	proxy      *httputil.ReverseProxy
}

func NewServer(config Config, repository Repository, logger *slog.Logger) *Server {
	if logger == nil {
		logger = slog.Default()
	}
	server := &Server{
		config:     config,
		repository: repository,
		logger:     logger,
		now:        func() time.Time { return time.Now().UTC() },
	}
	if strings.TrimSpace(config.DotnetUpstream) != "" {
		server.proxy = newReverseProxy(config.DotnetUpstream)
	}
	return server
}

func (server *Server) ServeHTTP(response http.ResponseWriter, request *http.Request) {
	switch {
	case request.Method == http.MethodGet && request.URL.Path == "/health":
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "go-city-service", "timestamp": server.now()})
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/cities/region-tabs":
		server.handleGetRegionTabs(response, request)
	case pathMatchesPrefix(request.URL.Path, "/api/v1/cities") && server.proxy != nil:
		server.proxy.ServeHTTP(response, request)
	default:
		writeJSON(response, http.StatusNotFound, ApiResponse[map[string]any]{Success: false, Message: "No route matched"})
	}
}

func (server *Server) handleGetRegionTabs(response http.ResponseWriter, request *http.Request) {
	ctx, cancel := context.WithTimeout(request.Context(), server.config.QueryTimeout)
	defer cancel()

	sources, err := server.repository.ListRegionSources(ctx)
	if err != nil {
		server.logger.Error("get city region tabs failed", "error", err)
		writeJSON(response, http.StatusInternalServerError, ApiResponse[[]CityRegionTab]{
			Success: false,
			Message: "An error occurred while retrieving city region tabs",
			Errors:  []string{err.Error()},
		})
		return
	}

	result := buildRegionTabs(sources)
	writeJSON(response, http.StatusOK, ApiResponse[[]CityRegionTab]{
		Success: true,
		Message: "City region tabs retrieved successfully",
		Data:    &result,
	})
}

func buildRegionTabs(sources []regionSource) []CityRegionTab {
	if len(sources) == 0 {
		return []CityRegionTab{}
	}
	counts := make(map[string]int)
	for _, source := range sources {
		region := resolveRegionBucket(source)
		if region == "" {
			continue
		}
		counts[region]++
	}

	result := make([]CityRegionTab, 0, len(counts))
	for region, count := range counts {
		result = append(result, CityRegionTab{
			Key:          region,
			Label:        normalizeRegionLabel(region),
			CityCount:    count,
			DisplayOrder: getRegionDisplayOrder(region),
		})
	}

	sort.Slice(result, func(index int, j int) bool {
		if result[index].DisplayOrder != result[j].DisplayOrder {
			return result[index].DisplayOrder < result[j].DisplayOrder
		}
		return strings.ToLower(result[index].Label) < strings.ToLower(result[j].Label)
	})
	return result
}

func resolveRegionBucket(source regionSource) string {
	if source.Continent != nil {
		normalized := normalizeRegionLabel(*source.Continent)
		if normalized != "" {
			return normalized
		}
	}
	if source.Region != nil {
		normalized := normalizeRegionLabel(*source.Region)
		if _, exists := regionDisplayOrders[normalized]; exists {
			return normalized
		}
	}
	return "Other"
}

func getRegionDisplayOrder(region string) int {
	if order, ok := regionDisplayOrders[region]; ok {
		return order
	}
	return 999
}

func normalizeRegionLabel(value string) string {
	cleaned := strings.TrimSpace(value)
	if cleaned == "" {
		return ""
	}

	replacer := strings.NewReplacer("_", " ", "-", " ")
	parts := strings.Fields(replacer.Replace(cleaned))
	if len(parts) == 0 {
		return cleaned
	}

	normalizedParts := make([]string, 0, len(parts))
	for _, part := range parts {
		if len(part) == 1 {
			normalizedParts = append(normalizedParts, strings.ToUpper(part))
			continue
		}
		normalizedParts = append(normalizedParts, strings.ToUpper(part[:1])+strings.ToLower(part[1:]))
	}
	return strings.Join(normalizedParts, " ")
}

func pathMatchesPrefix(path string, prefix string) bool {
	path = strings.TrimRight(path, "/")
	prefix = strings.TrimRight(prefix, "/")
	if path == prefix {
		return true
	}
	return strings.HasPrefix(path, prefix+"/")
}

func newReverseProxy(rawURL string) *httputil.ReverseProxy {
	target, err := url.Parse(rawURL)
	if err != nil {
		panic(err)
	}
	proxy := httputil.NewSingleHostReverseProxy(target)
	originalDirector := proxy.Director
	proxy.Director = func(request *http.Request) {
		originalDirector(request)
		request.Host = target.Host
	}
	return proxy
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}
