package main

import (
	"log/slog"
	"net/http"
	"os"
	"time"

	"github.com/go-nomads/backend/go-backend/internal/gateway"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	config := gateway.LoadConfigFromEnv()
	server := gateway.NewServer(config, logger)

	address := config.ListenAddress
	logger.Info("starting go gateway", "address", address)

	httpServer := &http.Server{
		Addr:              address,
		Handler:           server,
		ReadHeaderTimeout: 10 * time.Second,
	}

	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("gateway stopped", "error", err)
		os.Exit(1)
	}
}
