package main

import (
	"log/slog"
	"net/http"
	"os"
	"time"

	"github.com/go-nomads/backend/go-backend/internal/product"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	config := product.LoadConfigFromEnv()
	server := product.NewServer(config, product.NewMemoryRepository(), logger)

	logger.Info("starting go product service", "address", config.ListenAddress)

	httpServer := &http.Server{
		Addr:              config.ListenAddress,
		Handler:           server,
		ReadHeaderTimeout: 10 * time.Second,
	}

	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("product service stopped", "error", err)
		os.Exit(1)
	}
}
