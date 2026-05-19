package city

import (
	"context"
	"database/sql"
	"errors"
)

type Repository interface {
	ListRegionSources(ctx context.Context) ([]regionSource, error)
}

type PostgresRepository struct {
	database *sql.DB
}

func NewPostgresRepository(database *sql.DB) *PostgresRepository {
	return &PostgresRepository{database: database}
}

func (repository *PostgresRepository) ListRegionSources(ctx context.Context) ([]regionSource, error) {
	if repository.database == nil {
		return nil, errors.New("postgres database is not configured")
	}

	rows, err := repository.database.QueryContext(ctx, `
SELECT c.region, co.continent
FROM public.cities c
LEFT JOIN public.countries co ON co.id = c.country_id AND co.is_active = true
WHERE c.is_active = true`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	result := make([]regionSource, 0)
	for rows.Next() {
		var region sql.NullString
		var continent sql.NullString
		if err := rows.Scan(&region, &continent); err != nil {
			return nil, err
		}
		entry := regionSource{}
		if region.Valid {
			value := region.String
			entry.Region = &value
		}
		if continent.Valid {
			value := continent.String
			entry.Continent = &value
		}
		result = append(result, entry)
	}

	if err := rows.Err(); err != nil {
		return nil, err
	}

	return result, nil
}
