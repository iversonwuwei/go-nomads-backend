package cache

import "time"

type ScoreEntityType string

const (
	ScoreEntityCity      ScoreEntityType = "city"
	ScoreEntityCoworking ScoreEntityType = "coworking"
)

type CostEntityType string

const (
	CostEntityCity CostEntityType = "city"
)

type ScoreResponse struct {
	EntityID     string  `json:"entityId"`
	OverallScore float64 `json:"overallScore"`
	FromCache    bool    `json:"fromCache"`
	Statistics   *string `json:"statistics"`
}

type BatchScoreResponse struct {
	Scores          []ScoreResponse `json:"scores"`
	TotalCount      int             `json:"totalCount"`
	CachedCount     int             `json:"cachedCount"`
	CalculatedCount int             `json:"calculatedCount"`
}

type CostResponse struct {
	EntityID    string  `json:"entityId"`
	AverageCost float64 `json:"averageCost"`
	FromCache   bool    `json:"fromCache"`
	Statistics  *string `json:"statistics"`
}

type BatchCostResponse struct {
	Costs           []CostResponse `json:"costs"`
	TotalCount      int            `json:"totalCount"`
	CachedCount     int            `json:"cachedCount"`
	CalculatedCount int            `json:"calculatedCount"`
}

type scoreCacheData struct {
	OverallScore float64   `json:"overallScore"`
	CreatedAt    time.Time `json:"createdAt"`
	ExpiresAt    time.Time `json:"expiresAt"`
	Statistics   *string   `json:"statistics"`
}

func (entry scoreCacheData) expired(now time.Time) bool {
	return !entry.ExpiresAt.IsZero() && !now.Before(entry.ExpiresAt)
}

type costCacheData struct {
	AverageCost float64   `json:"averageCost"`
	CreatedAt   time.Time `json:"createdAt"`
	ExpiresAt   time.Time `json:"expiresAt"`
	Statistics  *string   `json:"statistics"`
}

func (entry costCacheData) expired(now time.Time) bool {
	return !entry.ExpiresAt.IsZero() && !now.Before(entry.ExpiresAt)
}

type categoryStatistic struct {
	Category      string  `json:"category,omitempty"`
	AverageRating float64 `json:"averageRating"`
	RatingCount   int     `json:"ratingCount"`
}

type cityRatingStatsResponse struct {
	Statistics []categoryStatistic `json:"statistics"`
}

type batchCityRatingStatsResponse struct {
	CityStatistics map[string]cityRatingStatsResponse `json:"cityStatistics"`
}

type cityCostStatsResponse struct {
	TotalAverageCost  float64            `json:"totalAverageCost"`
	CategoryCosts     map[string]float64 `json:"categoryCosts"`
	ContributorCount  int                `json:"contributorCount"`
	TotalExpenseCount int                `json:"totalExpenseCount"`
	Currency          string             `json:"currency"`
	UpdatedAt         string             `json:"updatedAt"`
}

type coworkingDetailResponse struct {
	ID          string  `json:"id"`
	Rating      float64 `json:"rating"`
	ReviewCount int     `json:"reviewCount"`
}
