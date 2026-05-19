package main

import (
	"log/slog"
	"net/http"
	"os"
	"time"

	"github.com/go-nomads/backend/go-backend/internal/cache"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	config := cache.LoadConfigFromEnv()
	repository := cache.NewRepository(config, logger)
	upstream := cache.NewHTTPUpstream(config, logger)
	server := cache.NewServer(config, repository, upstream, logger)

	logger.Info("starting go cache service", "address", config.ListenAddress)

	httpServer := &http.Server{
		Addr:              config.ListenAddress,
		Handler:           server,
		ReadHeaderTimeout: 10 * time.Second,
	}

	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("cache service stopped", "error", err)
		os.Exit(1)
	}
}
