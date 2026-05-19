package main

import (
	"database/sql"
	"log/slog"
	"net/http"
	"os"
	"time"

	_ "github.com/lib/pq"

	"github.com/go-nomads/backend/go-backend/internal/city"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	configuration := city.LoadConfigFromEnv()

	database, err := sql.Open("postgres", configuration.PostgresConnectionString)
	if err != nil {
		logger.Error("open postgres connection failed", "error", err)
		os.Exit(1)
	}
	defer database.Close()
	database.SetMaxOpenConns(configuration.MaxOpenConns)
	database.SetMaxIdleConns(configuration.MaxIdleConns)
	database.SetConnMaxLifetime(30 * time.Minute)

	repository := city.NewPostgresRepository(database)
	server := city.NewServer(configuration, repository, logger)
	logger.Info("starting go city service", "address", configuration.ListenAddress)

	httpServer := &http.Server{
		Addr:              configuration.ListenAddress,
		Handler:           server,
		ReadHeaderTimeout: 10 * time.Second,
	}

	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("city service stopped", "error", err)
		os.Exit(1)
	}
}
