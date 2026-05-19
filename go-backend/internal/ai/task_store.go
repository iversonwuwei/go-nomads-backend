package ai

import (
	"context"
	"encoding/json"
	"strings"
	"sync"
	"time"

	"github.com/redis/go-redis/v9"
)

type TaskStore interface {
	Set(status ImageTaskStatusResponse)
	Get(taskID string) (ImageTaskStatusResponse, bool)
}

type MemoryTaskStore struct {
	ttl   time.Duration
	lock  sync.RWMutex
	tasks map[string]ImageTaskStatusResponse
}

func NewMemoryTaskStore(ttl time.Duration) *MemoryTaskStore {
	return &MemoryTaskStore{
		ttl:   ttl,
		tasks: make(map[string]ImageTaskStatusResponse),
	}
}

func (store *MemoryTaskStore) Set(status ImageTaskStatusResponse) {
	store.lock.Lock()
	defer store.lock.Unlock()

	store.cleanupLocked(time.Now().UTC())
	store.tasks[status.TaskID] = status
}

func (store *MemoryTaskStore) Get(taskID string) (ImageTaskStatusResponse, bool) {
	store.lock.Lock()
	defer store.lock.Unlock()

	store.cleanupLocked(time.Now().UTC())
	status, ok := store.tasks[taskID]
	return status, ok
}

func (store *MemoryTaskStore) cleanupLocked(now time.Time) {
	if store.ttl <= 0 {
		return
	}

	for taskID, status := range store.tasks {
		if !status.CreatedAt.IsZero() && now.Sub(status.CreatedAt) > store.ttl {
			delete(store.tasks, taskID)
		}
	}
}

type RedisTaskStore struct {
	client *redis.Client
	ttl    time.Duration
	local  *MemoryTaskStore
}

func NewRedisTaskStore(connectionString string, ttl time.Duration, local *MemoryTaskStore) (*RedisTaskStore, error) {
	options, err := redisOptions(connectionString)
	if err != nil {
		return nil, err
	}
	client := redis.NewClient(options)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	if err := client.Ping(ctx).Err(); err != nil {
		_ = client.Close()
		return nil, err
	}
	return &RedisTaskStore{client: client, ttl: ttl, local: local}, nil
}

func (store *RedisTaskStore) Set(status ImageTaskStatusResponse) {
	store.local.Set(status)
	body, err := json.Marshal(status)
	if err != nil {
		return
	}
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_ = store.client.Set(ctx, taskKey(status.TaskID), body, store.ttl).Err()
}

func (store *RedisTaskStore) Get(taskID string) (ImageTaskStatusResponse, bool) {
	if status, ok := store.local.Get(taskID); ok {
		return status, true
	}
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	body, err := store.client.Get(ctx, taskKey(taskID)).Bytes()
	if err != nil {
		return ImageTaskStatusResponse{}, false
	}
	var status ImageTaskStatusResponse
	if err := json.Unmarshal(body, &status); err != nil {
		return ImageTaskStatusResponse{}, false
	}
	store.local.Set(status)
	return status, true
}

func taskKey(taskID string) string {
	return "task:image:" + taskID
}

func redisOptions(connectionString string) (*redis.Options, error) {
	value := strings.TrimSpace(connectionString)
	if strings.HasPrefix(value, "redis://") || strings.HasPrefix(value, "rediss://") {
		return redis.ParseURL(value)
	}
	address := strings.Split(value, ",")[0]
	return &redis.Options{Addr: address}, nil
}
