package product

import (
	"encoding/json"
	"log/slog"
	"math"
	"net/http"
	"strconv"
	"strings"
	"time"
)

type Server struct {
	config     Config
	logger     *slog.Logger
	repository Repository
	now        func() time.Time
}

func NewServer(config Config, repository Repository, logger *slog.Logger) *Server {
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
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "go-product-service", "timestamp": server.now()})
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/products":
		server.handleListProducts(response, request)
	case request.Method == http.MethodGet && request.URL.Path == "/api/v1/products/health":
		writeJSON(response, http.StatusOK, map[string]any{"status": "healthy", "service": "ProductService", "timestamp": server.now()})
	case request.Method == http.MethodGet && strings.HasPrefix(request.URL.Path, "/api/v1/products/user/"):
		server.handleListProductsByUser(response, request)
	case request.Method == http.MethodGet && strings.HasPrefix(request.URL.Path, "/api/v1/products/"):
		server.handleGetProduct(response, request)
	default:
		writeJSON(response, http.StatusNotFound, APIResponse[map[string]any]{Success: false, Message: "No route matched"})
	}
}

func (server *Server) handleListProducts(response http.ResponseWriter, request *http.Request) {
	page, pageSize := paginationFromRequest(request)
	products := server.repository.List()
	payload := paginateProducts(products, page, pageSize)
	writeJSON(response, http.StatusOK, APIResponse[PaginatedResponse[Product]]{Success: true, Message: "Products retrieved successfully", Data: &payload})
}

func (server *Server) handleGetProduct(response http.ResponseWriter, request *http.Request) {
	id := strings.TrimPrefix(request.URL.Path, "/api/v1/products/")
	if id == "" || id == "health" || strings.Contains(id, "/") {
		writeJSON(response, http.StatusNotFound, APIResponse[Product]{Success: false, Message: "Product not found"})
		return
	}

	product, ok := server.repository.GetByID(id)
	if !ok {
		writeJSON(response, http.StatusNotFound, APIResponse[Product]{Success: false, Message: "Product not found"})
		return
	}

	writeJSON(response, http.StatusOK, APIResponse[Product]{Success: true, Message: "Product retrieved successfully", Data: &product})
}

func (server *Server) handleListProductsByUser(response http.ResponseWriter, request *http.Request) {
	userID := strings.TrimPrefix(request.URL.Path, "/api/v1/products/user/")
	if userID == "" || strings.Contains(userID, "/") {
		writeJSON(response, http.StatusNotFound, APIResponse[PaginatedResponse[Product]]{Success: false, Message: "User not found"})
		return
	}

	page, pageSize := paginationFromRequest(request)
	products := server.repository.GetByUserID(userID)
	payload := paginateProducts(products, page, pageSize)
	writeJSON(response, http.StatusOK, APIResponse[PaginatedResponse[Product]]{Success: true, Message: "Products retrieved successfully", Data: &payload})
}

func paginationFromRequest(request *http.Request) (int, int) {
	page := intQueryOrDefault(request, "page", 1)
	pageSize := intQueryOrDefault(request, "pageSize", 10)
	if page < 1 {
		page = 1
	}
	if pageSize < 1 {
		pageSize = 1
	}
	if pageSize > 100 {
		pageSize = 100
	}
	return page, pageSize
}

func intQueryOrDefault(request *http.Request, key string, fallback int) int {
	value := strings.TrimSpace(request.URL.Query().Get(key))
	if value == "" {
		return fallback
	}
	parsed, err := strconv.Atoi(value)
	if err != nil {
		return fallback
	}
	return parsed
}

func paginateProducts(items []Product, page int, pageSize int) PaginatedResponse[Product] {
	skip := (page - 1) * pageSize
	if skip > len(items) {
		skip = len(items)
	}
	end := skip + pageSize
	if end > len(items) {
		end = len(items)
	}
	pagedItems := append([]Product(nil), items[skip:end]...)
	totalPages := 0
	if pageSize > 0 {
		totalPages = int(math.Ceil(float64(len(items)) / float64(pageSize)))
	}
	return PaginatedResponse[Product]{
		Items:      pagedItems,
		TotalCount: len(items),
		Page:       page,
		PageSize:   pageSize,
		TotalPages: totalPages,
	}
}

func mustTime(value string) time.Time {
	parsed, err := time.Parse(time.RFC3339, value)
	if err != nil {
		panic(err)
	}
	return parsed
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)
	_ = json.NewEncoder(response).Encode(payload)
}
