package main

import (
	"log/slog"
	"net/http"
	"os"
	"time"

	"github.com/go-nomads/backend/go-backend/internal/search"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	config := search.LoadConfigFromEnv()
	client := search.NewElasticsearchClient(config, logger)
	server := search.NewServer(config, client, logger)

	logger.Info("starting go search service", "address", config.ListenAddress)

	httpServer := &http.Server{
		Addr:              config.ListenAddress,
		Handler:           server,
		ReadHeaderTimeout: 10 * time.Second,
	}

	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("search service stopped", "error", err)
		os.Exit(1)
	}
}
