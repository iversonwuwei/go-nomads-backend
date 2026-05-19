package cache

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"strings"
	"sync"
	"time"

	"github.com/redis/go-redis/v9"
)

type Repository interface {
	GetScore(ctx context.Context, entityType ScoreEntityType, entityID string) (scoreCacheData, bool)
	GetScoresBatch(ctx context.Context, entityType ScoreEntityType, entityIDs []string) map[string]scoreCacheData
	SetScore(ctx context.Context, entityType ScoreEntityType, entityID string, entry scoreCacheData)
	SetScores(ctx context.Context, entityType ScoreEntityType, entries map[string]scoreCacheData)
	GetCost(ctx context.Context, entityType CostEntityType, entityID string) (costCacheData, bool)
	GetCostsBatch(ctx context.Context, entityType CostEntityType, entityIDs []string) map[string]costCacheData
	SetCost(ctx context.Context, entityType CostEntityType, entityID string, entry costCacheData)
	SetCosts(ctx context.Context, entityType CostEntityType, entries map[string]costCacheData)
}

func NewRepository(config Config, logger *slog.Logger) Repository {
	if strings.TrimSpace(config.RedisConnectionString) == "" {
		return NewMemoryRepository()
	}

	options := redisOptionsFromConnectionString(config.RedisConnectionString)
	client := redis.NewClient(options)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	if err := client.Ping(ctx).Err(); err != nil {
		if logger != nil {
			logger.Warn("redis unavailable for go cache service, falling back to memory", "error", err)
		}
		return NewMemoryRepository()
	}
	return &RedisRepository{client: client, logger: logger}
}

func redisOptionsFromConnectionString(connectionString string) *redis.Options {
	trimmed := strings.TrimSpace(connectionString)
	if strings.Contains(trimmed, "://") {
		if options, err := redis.ParseURL(trimmed); err == nil {
			return options
		}
	}
	addr := trimmed
	if index := strings.Index(addr, ","); index >= 0 {
		addr = addr[:index]
	}
	return &redis.Options{Addr: strings.TrimSpace(addr)}
}

type MemoryRepository struct {
	mu     sync.RWMutex
	scores map[string]scoreCacheData
	costs  map[string]costCacheData
}

func NewMemoryRepository() *MemoryRepository {
	return &MemoryRepository{
		scores: map[string]scoreCacheData{},
		costs:  map[string]costCacheData{},
	}
}

func (repository *MemoryRepository) GetScore(_ context.Context, entityType ScoreEntityType, entityID string) (scoreCacheData, bool) {
	repository.mu.RLock()
	defer repository.mu.RUnlock()
	entry, ok := repository.scores[scoreKey(entityType, entityID)]
	return entry, ok
}

func (repository *MemoryRepository) GetScoresBatch(_ context.Context, entityType ScoreEntityType, entityIDs []string) map[string]scoreCacheData {
	repository.mu.RLock()
	defer repository.mu.RUnlock()
	result := make(map[string]scoreCacheData, len(entityIDs))
	for _, entityID := range entityIDs {
		if entry, ok := repository.scores[scoreKey(entityType, entityID)]; ok {
			result[entityID] = entry
		}
	}
	return result
}

func (repository *MemoryRepository) SetScore(_ context.Context, entityType ScoreEntityType, entityID string, entry scoreCacheData) {
	repository.mu.Lock()
	defer repository.mu.Unlock()
	repository.scores[scoreKey(entityType, entityID)] = entry
}

func (repository *MemoryRepository) SetScores(_ context.Context, entityType ScoreEntityType, entries map[string]scoreCacheData) {
	repository.mu.Lock()
	defer repository.mu.Unlock()
	for entityID, entry := range entries {
		repository.scores[scoreKey(entityType, entityID)] = entry
	}
}

func (repository *MemoryRepository) GetCost(_ context.Context, entityType CostEntityType, entityID string) (costCacheData, bool) {
	repository.mu.RLock()
	defer repository.mu.RUnlock()
	entry, ok := repository.costs[costKey(entityType, entityID)]
	return entry, ok
}

func (repository *MemoryRepository) GetCostsBatch(_ context.Context, entityType CostEntityType, entityIDs []string) map[string]costCacheData {
	repository.mu.RLock()
	defer repository.mu.RUnlock()
	result := make(map[string]costCacheData, len(entityIDs))
	for _, entityID := range entityIDs {
		if entry, ok := repository.costs[costKey(entityType, entityID)]; ok {
			result[entityID] = entry
		}
	}
	return result
}

func (repository *MemoryRepository) SetCost(_ context.Context, entityType CostEntityType, entityID string, entry costCacheData) {
	repository.mu.Lock()
	defer repository.mu.Unlock()
	repository.costs[costKey(entityType, entityID)] = entry
}

func (repository *MemoryRepository) SetCosts(_ context.Context, entityType CostEntityType, entries map[string]costCacheData) {
	repository.mu.Lock()
	defer repository.mu.Unlock()
	for entityID, entry := range entries {
		repository.costs[costKey(entityType, entityID)] = entry
	}
}

type RedisRepository struct {
	client *redis.Client
	logger *slog.Logger
}

func (repository *RedisRepository) GetScore(ctx context.Context, entityType ScoreEntityType, entityID string) (scoreCacheData, bool) {
	value, err := repository.client.Get(ctx, scoreKey(entityType, entityID)).Bytes()
	if err != nil {
		return scoreCacheData{}, false
	}
	var entry scoreCacheData
	if err := json.Unmarshal(value, &entry); err != nil {
		repository.log("failed to decode score cache entry", err)
		return scoreCacheData{}, false
	}
	return entry, true
}

func (repository *RedisRepository) GetScoresBatch(ctx context.Context, entityType ScoreEntityType, entityIDs []string) map[string]scoreCacheData {
	result := make(map[string]scoreCacheData, len(entityIDs))
	if len(entityIDs) == 0 {
		return result
	}
	keys := make([]string, 0, len(entityIDs))
	for _, entityID := range entityIDs {
		keys = append(keys, scoreKey(entityType, entityID))
	}
	values, err := repository.client.MGet(ctx, keys...).Result()
	if err != nil {
		repository.log("failed to fetch score cache batch", err)
		return result
	}
	for index, value := range values {
		if value == nil {
			continue
		}
		text, ok := value.(string)
		if !ok {
			continue
		}
		var entry scoreCacheData
		if err := json.Unmarshal([]byte(text), &entry); err != nil {
			repository.log("failed to decode score cache batch entry", err)
			continue
		}
		result[entityIDs[index]] = entry
	}
	return result
}

func (repository *RedisRepository) SetScore(ctx context.Context, entityType ScoreEntityType, entityID string, entry scoreCacheData) {
	payload, err := json.Marshal(entry)
	if err != nil {
		repository.log("failed to encode score cache entry", err)
		return
	}
	ttl := time.Until(entry.ExpiresAt)
	if ttl <= 0 {
		ttl = time.Second
	}
	if err := repository.client.Set(ctx, scoreKey(entityType, entityID), payload, ttl).Err(); err != nil {
		repository.log("failed to store score cache entry", err)
	}
}

func (repository *RedisRepository) SetScores(ctx context.Context, entityType ScoreEntityType, entries map[string]scoreCacheData) {
	for entityID, entry := range entries {
		repository.SetScore(ctx, entityType, entityID, entry)
	}
}

func (repository *RedisRepository) GetCost(ctx context.Context, entityType CostEntityType, entityID string) (costCacheData, bool) {
	value, err := repository.client.Get(ctx, costKey(entityType, entityID)).Bytes()
	if err != nil {
		return costCacheData{}, false
	}
	var entry costCacheData
	if err := json.Unmarshal(value, &entry); err != nil {
		repository.log("failed to decode cost cache entry", err)
		return costCacheData{}, false
	}
	return entry, true
}

func (repository *RedisRepository) GetCostsBatch(ctx context.Context, entityType CostEntityType, entityIDs []string) map[string]costCacheData {
	result := make(map[string]costCacheData, len(entityIDs))
	if len(entityIDs) == 0 {
		return result
	}
	keys := make([]string, 0, len(entityIDs))
	for _, entityID := range entityIDs {
		keys = append(keys, costKey(entityType, entityID))
	}
	values, err := repository.client.MGet(ctx, keys...).Result()
	if err != nil {
		repository.log("failed to fetch cost cache batch", err)
		return result
	}
	for index, value := range values {
		if value == nil {
			continue
		}
		text, ok := value.(string)
		if !ok {
			continue
		}
		var entry costCacheData
		if err := json.Unmarshal([]byte(text), &entry); err != nil {
			repository.log("failed to decode cost cache batch entry", err)
			continue
		}
		result[entityIDs[index]] = entry
	}
	return result
}

func (repository *RedisRepository) SetCost(ctx context.Context, entityType CostEntityType, entityID string, entry costCacheData) {
	payload, err := json.Marshal(entry)
	if err != nil {
		repository.log("failed to encode cost cache entry", err)
		return
	}
	ttl := time.Until(entry.ExpiresAt)
	if ttl <= 0 {
		ttl = time.Second
	}
	if err := repository.client.Set(ctx, costKey(entityType, entityID), payload, ttl).Err(); err != nil {
		repository.log("failed to store cost cache entry", err)
	}
}

func (repository *RedisRepository) SetCosts(ctx context.Context, entityType CostEntityType, entries map[string]costCacheData) {
	for entityID, entry := range entries {
		repository.SetCost(ctx, entityType, entityID, entry)
	}
}

func (repository *RedisRepository) log(message string, err error) {
	if repository.logger != nil {
		repository.logger.Warn(message, "error", err)
	}
}

func scoreKey(entityType ScoreEntityType, entityID string) string {
	return fmt.Sprintf("%s:score:%s", entityType, entityID)
}

func costKey(entityType CostEntityType, entityID string) string {
	return fmt.Sprintf("%s:cost:%s", entityType, entityID)
}
