package search

type ApiResponse[T any] struct {
	Success bool    `json:"success"`
	Message *string `json:"message,omitempty"`
	Data    *T      `json:"data,omitempty"`
}

type SearchRequest struct {
	Query       string
	Page        int
	PageSize    int
	Type        string
	Country     string
	CityID      string
	MinRating   *float64
	SortBy      string
	SortOrder   string
	Latitude    *float64
	Longitude   *float64
	RadiusKm    *float64
	EnableFuzzy bool
}

type SuggestRequest struct {
	Prefix string
	Type   string
	Size   int
}

type SearchResult[T any] struct {
	Items       []SearchResultItem[T] `json:"items"`
	TotalCount  int64                 `json:"totalCount"`
	Took        int64                 `json:"took"`
	Page        int                   `json:"page"`
	PageSize    int                   `json:"pageSize"`
	TotalPages  int                   `json:"totalPages"`
	HasMore     bool                  `json:"hasMore"`
	Suggestions []string              `json:"suggestions,omitempty"`
}

type SearchResultItem[T any] struct {
	Document   T                   `json:"document"`
	Score      *float64            `json:"score,omitempty"`
	Highlights map[string][]string `json:"highlights,omitempty"`
}

type UnifiedSearchResult struct {
	Cities     SearchResult[CitySearchDocument]      `json:"cities"`
	Coworkings SearchResult[CoworkingSearchDocument] `json:"coworkings"`
	TotalTook  int64                                 `json:"totalTook"`
	TotalCount int64                                 `json:"totalCount"`
}

type SuggestResponse struct {
	Suggestions []SuggestItem `json:"suggestions"`
}

type SuggestItem struct {
	Text     string         `json:"text"`
	ID       string         `json:"id"`
	Type     string         `json:"type"`
	Score    float64        `json:"score"`
	Metadata map[string]any `json:"metadata,omitempty"`
}

type GeoLocation struct {
	Lat float64 `json:"lat"`
	Lon float64 `json:"lon"`
}

type CitySearchDocument struct {
	ID                   string       `json:"id"`
	Name                 string       `json:"name"`
	NameEn               *string      `json:"nameEn,omitempty"`
	Country              string       `json:"country"`
	CountryID            *string      `json:"countryId,omitempty"`
	ProvinceID           *string      `json:"provinceId,omitempty"`
	Region               *string      `json:"region,omitempty"`
	Description          *string      `json:"description,omitempty"`
	Latitude             *float64     `json:"latitude,omitempty"`
	Longitude            *float64     `json:"longitude,omitempty"`
	Location             *GeoLocation `json:"location,omitempty"`
	Population           *int64       `json:"population,omitempty"`
	Climate              *string      `json:"climate,omitempty"`
	TimeZone             *string      `json:"timeZone,omitempty"`
	Currency             *string      `json:"currency,omitempty"`
	ImageURL             *string      `json:"imageUrl,omitempty"`
	PortraitImageURL     *string      `json:"portraitImageUrl,omitempty"`
	OverallScore         *float64     `json:"overallScore,omitempty"`
	InternetQualityScore *float64     `json:"internetQualityScore,omitempty"`
	SafetyScore          *float64     `json:"safetyScore,omitempty"`
	CostScore            *float64     `json:"costScore,omitempty"`
	CommunityScore       *float64     `json:"communityScore,omitempty"`
	WeatherScore         *float64     `json:"weatherScore,omitempty"`
	Tags                 []string     `json:"tags,omitempty"`
	IsActive             bool         `json:"isActive"`
	CreatedAt            string       `json:"createdAt"`
	UpdatedAt            *string      `json:"updatedAt,omitempty"`
	Suggest              *string      `json:"suggest,omitempty"`
	DocumentType         string       `json:"documentType"`
	AverageCost          *float64     `json:"averageCost,omitempty"`
	UserCount            int          `json:"userCount"`
	ModeratorID          *string      `json:"moderatorId,omitempty"`
	ModeratorName        *string      `json:"moderatorName,omitempty"`
	ModeratorCount       int          `json:"moderatorCount"`
	CoworkingCount       int          `json:"coworkingCount"`
	MeetupCount          int          `json:"meetupCount"`
	ReviewCount          int          `json:"reviewCount"`
}

type CoworkingSearchDocument struct {
	ID                 string       `json:"id"`
	Name               string       `json:"name"`
	CityID             *string      `json:"cityId,omitempty"`
	CityName           *string      `json:"cityName,omitempty"`
	CountryName        *string      `json:"countryName,omitempty"`
	Address            string       `json:"address"`
	Description        *string      `json:"description,omitempty"`
	ImageURL           *string      `json:"imageUrl,omitempty"`
	PricePerDay        *float64     `json:"pricePerDay,omitempty"`
	PricePerMonth      *float64     `json:"pricePerMonth,omitempty"`
	PricePerHour       *float64     `json:"pricePerHour,omitempty"`
	Currency           string       `json:"currency"`
	Rating             float64      `json:"rating"`
	ReviewCount        int          `json:"reviewCount"`
	WifiSpeed          *float64     `json:"wifiSpeed,omitempty"`
	Desks              *int         `json:"desks,omitempty"`
	MeetingRooms       *int         `json:"meetingRooms,omitempty"`
	HasMeetingRoom     bool         `json:"hasMeetingRoom,omitempty"`
	HasCoffee          bool         `json:"hasCoffee,omitempty"`
	HasParking         bool         `json:"hasParking,omitempty"`
	Has247Access       bool         `json:"has247Access,omitempty"`
	Amenities          []string     `json:"amenities,omitempty"`
	Capacity           *int         `json:"capacity,omitempty"`
	Latitude           *float64     `json:"latitude,omitempty"`
	Longitude          *float64     `json:"longitude,omitempty"`
	Location           *GeoLocation `json:"location,omitempty"`
	Phone              *string      `json:"phone,omitempty"`
	Email              *string      `json:"email,omitempty"`
	Website            *string      `json:"website,omitempty"`
	OpeningHours       *string      `json:"openingHours,omitempty"`
	IsActive           bool         `json:"isActive"`
	VerificationStatus string       `json:"verificationStatus"`
	CreatedAt          string       `json:"createdAt"`
	UpdatedAt          string       `json:"updatedAt"`
	Suggest            *string      `json:"suggest,omitempty"`
	DocumentType       string       `json:"documentType"`
}

func ptrString(value string) *string { return &value }
