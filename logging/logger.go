// Package logging provides environment-configurable structured logging.
package logging

import (
	"io"
	"log/slog"
	"os"
	"strings"
)

const (
	LevelEnv  = "LOG_LEVEL"
	FormatEnv = "LOG_FORMAT"
)

// NewFromEnv creates a logger using LOG_LEVEL and LOG_FORMAT. Unknown or empty
// values fall back to info and text, respectively. A nil writer uses stderr.
func NewFromEnv(w io.Writer) *slog.Logger {
	if w == nil {
		w = os.Stderr
	}

	var level slog.Level
	switch strings.ToLower(strings.TrimSpace(os.Getenv(LevelEnv))) {
	case "debug":
		level = slog.LevelDebug
	case "warn", "warning":
		level = slog.LevelWarn
	case "error":
		level = slog.LevelError
	default:
		level = slog.LevelInfo
	}

	opts := &slog.HandlerOptions{Level: level}
	if strings.EqualFold(strings.TrimSpace(os.Getenv(FormatEnv)), "json") {
		return slog.New(slog.NewJSONHandler(w, opts))
	}
	return slog.New(slog.NewTextHandler(w, opts))
}
