package search

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"net/url"
	"sort"
	"strings"
)

type Client interface {
	SearchCities(ctx context.Context, request SearchRequest) (SearchResult[CitySearchDocument], error)
	SearchCoworkings(ctx context.Context, request SearchRequest) (SearchResult[CoworkingSearchDocument], error)
	SuggestCities(ctx context.Context, request SuggestRequest) ([]SuggestItem, error)
	SuggestCoworkings(ctx context.Context, request SuggestRequest) ([]SuggestItem, error)
}

type ElasticsearchClient struct {
	config     Config
	logger     *slog.Logger
	httpClient *http.Client
}

func NewElasticsearchClient(config Config, logger *slog.Logger) *ElasticsearchClient {
	if logger == nil {
		logger = slog.Default()
	}
	return &ElasticsearchClient{config: config, logger: logger, httpClient: &http.Client{Timeout: config.RequestTimeout}}
}

func (client *ElasticsearchClient) SearchCities(ctx context.Context, request SearchRequest) (SearchResult[CitySearchDocument], error) {
	return searchIndex[CitySearchDocument](ctx, client, client.config.CityIndex, request, citySearchFields(), citySearchFilterFields())
}

func (client *ElasticsearchClient) SearchCoworkings(ctx context.Context, request SearchRequest) (SearchResult[CoworkingSearchDocument], error) {
	return searchIndex[CoworkingSearchDocument](ctx, client, client.config.CoworkingIndex, request, coworkingSearchFields(), coworkingSearchFilterFields())
}

func (client *ElasticsearchClient) SuggestCities(ctx context.Context, request SuggestRequest) ([]SuggestItem, error) {
	result, err := searchIndex[CitySearchDocument](ctx, client, client.config.CityIndex, SearchRequest{Query: request.Prefix, Page: 1, PageSize: request.Size, EnableFuzzy: true}, citySearchFields(), nil)
	if err != nil {
		return nil, err
	}
	items := make([]SuggestItem, 0, len(result.Items))
	for _, item := range result.Items {
		items = append(items, SuggestItem{Text: item.Document.Name, ID: item.Document.ID, Type: "city", Score: derefScore(item.Score), Metadata: map[string]any{"country": item.Document.Country}})
	}
	return items, nil
}

func (client *ElasticsearchClient) SuggestCoworkings(ctx context.Context, request SuggestRequest) ([]SuggestItem, error) {
	result, err := searchIndex[CoworkingSearchDocument](ctx, client, client.config.CoworkingIndex, SearchRequest{Query: request.Prefix, Page: 1, PageSize: request.Size, EnableFuzzy: true}, coworkingSearchFields(), nil)
	if err != nil {
		return nil, err
	}
	items := make([]SuggestItem, 0, len(result.Items))
	for _, item := range result.Items {
		metadata := map[string]any{}
		if item.Document.CityName != nil {
			metadata["cityName"] = *item.Document.CityName
		}
		items = append(items, SuggestItem{Text: item.Document.Name, ID: item.Document.ID, Type: "coworking", Score: derefScore(item.Score), Metadata: metadata})
	}
	return items, nil
}

func searchIndex[T any](ctx context.Context, client *ElasticsearchClient, index string, request SearchRequest, fields []string, _ map[string]string) (SearchResult[T], error) {
	page := request.Page
	if page < 1 {
		page = 1
	}
	pageSize := request.PageSize
	if pageSize < 1 {
		pageSize = 20
	}
	if pageSize > 100 {
		pageSize = 100
	}
	body := buildSearchBody(index, request, page, pageSize, fields)
	response, err := client.postSearch(ctx, index, body)
	if err != nil {
		return SearchResult[T]{Page: page, PageSize: pageSize}, err
	}
	return decodeSearchResponse[T](response, page, pageSize)
}

func (client *ElasticsearchClient) postSearch(ctx context.Context, index string, body map[string]any) ([]byte, error) {
	if strings.TrimSpace(client.config.ElasticsearchURL) == "" {
		return nil, fmt.Errorf("missing Elasticsearch__Url")
	}
	payload, err := json.Marshal(body)
	if err != nil {
		return nil, err
	}
	baseURL := strings.TrimRight(client.config.ElasticsearchURL, "/")
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, baseURL+"/"+url.PathEscape(index)+"/_search", bytes.NewReader(payload))
	if err != nil {
		return nil, err
	}
	request.Header.Set("Content-Type", "application/json")
	response, err := client.httpClient.Do(request)
	if err != nil {
		return nil, err
	}
	defer response.Body.Close()
	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return nil, fmt.Errorf("unexpected search status %d", response.StatusCode)
	}
	return ioReadAll(response.Body)
}

func buildSearchBody(index string, request SearchRequest, page int, pageSize int, fields []string) map[string]any {
	from := (page - 1) * pageSize
	must := make([]any, 0, 1)
	filters := make([]any, 0, 3)
	query := strings.TrimSpace(request.Query)
	if query == "" {
		must = append(must, map[string]any{"match_all": map[string]any{}})
	} else {
		multiMatch := map[string]any{
			"query":  query,
			"fields": fields,
		}
		if request.EnableFuzzy {
			multiMatch["fuzziness"] = "AUTO"
		}
		must = append(must, map[string]any{"multi_match": multiMatch})
	}
	if request.Country != "" {
		field := "country.keyword"
		if index == "coworking_spaces" {
			field = "countryName.keyword"
		}
		filters = append(filters, map[string]any{"term": map[string]any{field: request.Country}})
	}
	if request.CityID != "" && index == "coworking_spaces" {
		filters = append(filters, map[string]any{"term": map[string]any{"cityId": request.CityID}})
	}
	if request.MinRating != nil {
		field := "overallScore"
		if index == "coworking_spaces" {
			field = "rating"
		}
		filters = append(filters, map[string]any{"range": map[string]any{field: map[string]any{"gte": *request.MinRating}}})
	}
	if request.Latitude != nil && request.Longitude != nil && request.RadiusKm != nil {
		filters = append(filters, map[string]any{"geo_distance": map[string]any{"distance": fmt.Sprintf("%gkm", *request.RadiusKm), "location": map[string]any{"lat": *request.Latitude, "lon": *request.Longitude}}})
	}
	body := map[string]any{
		"from": from,
		"size": pageSize,
		"query": map[string]any{
			"bool": map[string]any{
				"must": must,
			},
		},
	}
	if len(filters) > 0 {
		body["query"].(map[string]any)["bool"].(map[string]any)["filter"] = filters
	}
	if request.SortBy != "" {
		order := request.SortOrder
		if order == "" {
			order = "desc"
		}
		body["sort"] = []any{map[string]any{request.SortBy: map[string]any{"order": order}}}
	}
	body["highlight"] = map[string]any{"fields": map[string]any{"name": map[string]any{}, "description": map[string]any{}, "country": map[string]any{}, "cityName": map[string]any{}, "address": map[string]any{}}}
	return body
}

type elasticsearchSearchResponse[T any] struct {
	Took int64 `json:"took"`
	Hits struct {
		Total struct {
			Value int64 `json:"value"`
		} `json:"total"`
		Hits []struct {
			Source    T                   `json:"_source"`
			Score     *float64            `json:"_score"`
			Highlight map[string][]string `json:"highlight"`
		} `json:"hits"`
	} `json:"hits"`
}

func decodeSearchResponse[T any](payload []byte, page int, pageSize int) (SearchResult[T], error) {
	var response elasticsearchSearchResponse[T]
	if err := json.Unmarshal(payload, &response); err != nil {
		return SearchResult[T]{Page: page, PageSize: pageSize}, err
	}
	items := make([]SearchResultItem[T], 0, len(response.Hits.Hits))
	for _, hit := range response.Hits.Hits {
		items = append(items, SearchResultItem[T]{Document: hit.Source, Score: hit.Score, Highlights: hit.Highlight})
	}
	totalPages := 0
	if pageSize > 0 {
		totalPages = int((response.Hits.Total.Value + int64(pageSize) - 1) / int64(pageSize))
	}
	return SearchResult[T]{
		Items:      items,
		TotalCount: response.Hits.Total.Value,
		Took:       response.Took,
		Page:       page,
		PageSize:   pageSize,
		TotalPages: totalPages,
		HasMore:    page < totalPages,
	}, nil
}

func citySearchFields() []string {
	return []string{"name^3", "nameEn^3", "description", "country", "region"}
}

func coworkingSearchFields() []string {
	return []string{"name^3", "description", "address", "cityName", "countryName"}
}

func citySearchFilterFields() map[string]string      { return nil }
func coworkingSearchFilterFields() map[string]string { return nil }

func derefScore(score *float64) float64 {
	if score == nil {
		return 0
	}
	return *score
}

func ioReadAll(body io.Reader) ([]byte, error) {
	buffer := new(bytes.Buffer)
	if _, err := buffer.ReadFrom(body); err != nil {
		return nil, err
	}
	return buffer.Bytes(), nil
}

type StubClient struct {
	Cities      SearchResult[CitySearchDocument]
	Coworkings  SearchResult[CoworkingSearchDocument]
	CitySuggest []SuggestItem
	CowSuggest  []SuggestItem
	Err         error
}

func (client *StubClient) SearchCities(_ context.Context, _ SearchRequest) (SearchResult[CitySearchDocument], error) {
	return client.Cities, client.Err
}

func (client *StubClient) SearchCoworkings(_ context.Context, _ SearchRequest) (SearchResult[CoworkingSearchDocument], error) {
	return client.Coworkings, client.Err
}

func (client *StubClient) SuggestCities(_ context.Context, _ SuggestRequest) ([]SuggestItem, error) {
	return client.CitySuggest, client.Err
}

func (client *StubClient) SuggestCoworkings(_ context.Context, _ SuggestRequest) ([]SuggestItem, error) {
	return client.CowSuggest, client.Err
}

func mergeSuggestions(size int, groups ...[]SuggestItem) []SuggestItem {
	merged := make([]SuggestItem, 0)
	for _, group := range groups {
		merged = append(merged, group...)
	}
	sort.SliceStable(merged, func(i int, j int) bool { return merged[i].Score > merged[j].Score })
	if size > 0 && len(merged) > size {
		merged = merged[:size]
	}
	return merged
}
