package main

import (
	"log/slog"
	"net/http"
	"os"
	"time"

	"github.com/go-nomads/backend/go-backend/internal/ai"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	config := ai.LoadConfigFromEnv()
	server := ai.NewServer(config, logger)

	logger.Info("starting go ai image service", "address", config.ListenAddress, "sidecarUrl", config.SidecarURL)

	httpServer := &http.Server{
		Addr:              config.ListenAddress,
		Handler:           server,
		ReadHeaderTimeout: 10 * time.Second,
	}

	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("ai image service stopped", "error", err)
		os.Exit(1)
	}
}
