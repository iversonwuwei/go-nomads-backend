package gateway

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"errors"
	"fmt"
	"strings"
	"time"
)

var (
	ErrMissingToken = errors.New("missing bearer token")
	ErrInvalidToken = errors.New("invalid bearer token")
)

type Claims struct {
	Subject  string
	Email    string
	Role     string
	Issuer   string
	Audience string
}

type tokenPayload struct {
	Subject  string      `json:"sub"`
	Email    string      `json:"email"`
	Role     string      `json:"role"`
	Issuer   string      `json:"iss"`
	Audience interface{} `json:"aud"`
	Expires  float64     `json:"exp"`
}

func validateBearerToken(headerValue string, config Config, now time.Time) (Claims, error) {
	token := strings.TrimSpace(headerValue)
	if token == "" {
		return Claims{}, ErrMissingToken
	}

	if strings.HasPrefix(strings.ToLower(token), "bearer ") {
		token = strings.TrimSpace(token[7:])
	}

	parts := strings.Split(token, ".")
	if len(parts) != 3 || config.JWTSecret == "" {
		return Claims{}, ErrInvalidToken
	}

	signedContent := parts[0] + "." + parts[1]
	signature, err := base64.RawURLEncoding.DecodeString(parts[2])
	if err != nil {
		return Claims{}, ErrInvalidToken
	}

	mac := hmac.New(sha256.New, []byte(config.JWTSecret))
	_, _ = mac.Write([]byte(signedContent))
	if !hmac.Equal(signature, mac.Sum(nil)) {
		return Claims{}, ErrInvalidToken
	}

	payloadBytes, err := base64.RawURLEncoding.DecodeString(parts[1])
	if err != nil {
		return Claims{}, ErrInvalidToken
	}

	var payload tokenPayload
	if err := json.Unmarshal(payloadBytes, &payload); err != nil {
		return Claims{}, ErrInvalidToken
	}

	if payload.Expires > 0 && now.Unix() > int64(payload.Expires) {
		return Claims{}, fmt.Errorf("%w: expired", ErrInvalidToken)
	}

	if config.JWTIssuer != "" && payload.Issuer != config.JWTIssuer {
		return Claims{}, fmt.Errorf("%w: issuer", ErrInvalidToken)
	}

	audience := normalizeAudience(payload.Audience)
	if config.JWTAudience != "" && audience != config.JWTAudience {
		return Claims{}, fmt.Errorf("%w: audience", ErrInvalidToken)
	}

	return Claims{
		Subject:  payload.Subject,
		Email:    payload.Email,
		Role:     payload.Role,
		Issuer:   payload.Issuer,
		Audience: audience,
	}, nil
}

func normalizeAudience(value interface{}) string {
	switch typed := value.(type) {
	case string:
		return typed
	case []interface{}:
		if len(typed) == 0 {
			return ""
		}
		if text, ok := typed[0].(string); ok {
			return text
		}
	}

	return ""
}
