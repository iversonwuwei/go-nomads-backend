package config

import (
	"context"
	"database/sql"
	"errors"
)

type SnapshotRepository interface {
	GetPublished(ctx context.Context) (publishedSnapshot, error)
}

type PostgresSnapshotRepository struct {
	database *sql.DB
}

func NewPostgresSnapshotRepository(database *sql.DB) *PostgresSnapshotRepository {
	return &PostgresSnapshotRepository{database: database}
}

func (repository *PostgresSnapshotRepository) GetPublished(ctx context.Context) (publishedSnapshot, error) {
	if repository.database == nil {
		return publishedSnapshot{}, errors.New("postgres database is not configured")
	}

	row := repository.database.QueryRowContext(ctx, `
SELECT version, static_texts, option_groups, system_settings, published_at
FROM public.app_config_snapshots
WHERE is_published = true AND is_deleted = false
ORDER BY version DESC
LIMIT 1`)

	var snapshot publishedSnapshot
	if err := row.Scan(&snapshot.Version, &snapshot.StaticTexts, &snapshot.OptionGroups, &snapshot.SystemSettings, &snapshot.PublishedAt); err != nil {
		return publishedSnapshot{}, err
	}

	return snapshot, nil
}
