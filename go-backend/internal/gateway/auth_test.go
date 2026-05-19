package gateway

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"testing"
	"time"
)

func TestValidateBearerToken(t *testing.T) {
	config := DefaultConfig()
	config.JWTSecret = "secret"
	config.JWTIssuer = "issuer"
	config.JWTAudience = "authenticated"

	token := signedToken(t, config.JWTSecret, map[string]any{
		"sub":   "user-1",
		"email": "user@example.com",
		"role":  "admin",
		"iss":   "issuer",
		"aud":   "authenticated",
		"exp":   time.Now().Add(time.Hour).Unix(),
	})

	claims, err := validateBearerToken("Bearer "+token, config, time.Now())
	if err != nil {
		t.Fatalf("expected token to validate: %v", err)
	}

	if claims.Subject != "user-1" || claims.Email != "user@example.com" || claims.Role != "admin" {
		t.Fatalf("unexpected claims: %+v", claims)
	}
}

func TestValidateBearerTokenRejectsInvalidSignature(t *testing.T) {
	config := DefaultConfig()
	config.JWTSecret = "secret"

	token := signedToken(t, "wrong-secret", map[string]any{
		"sub": "user-1",
		"exp": time.Now().Add(time.Hour).Unix(),
	})

	if _, err := validateBearerToken(token, config, time.Now()); err == nil {
		t.Fatal("expected invalid signature to fail")
	}
}

func signedToken(t *testing.T, secret string, payload map[string]any) string {
	t.Helper()

	headerBytes, err := json.Marshal(map[string]string{"alg": "HS256", "typ": "JWT"})
	if err != nil {
		t.Fatal(err)
	}
	payloadBytes, err := json.Marshal(payload)
	if err != nil {
		t.Fatal(err)
	}

	header := base64.RawURLEncoding.EncodeToString(headerBytes)
	body := base64.RawURLEncoding.EncodeToString(payloadBytes)
	signedContent := header + "." + body

	mac := hmac.New(sha256.New, []byte(secret))
	_, _ = mac.Write([]byte(signedContent))
	signature := base64.RawURLEncoding.EncodeToString(mac.Sum(nil))

	return signedContent + "." + signature
}
