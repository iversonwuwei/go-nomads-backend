package gateway

import "testing"

func TestParseAssignmentsSupportsMultipleSeparators(t *testing.T) {
	assignments := parseAssignments("go-city=http://go-city:5202; dotnet-city=http://city:5202\n/api/v1/cities=go-city")

	if len(assignments) != 3 {
		t.Fatalf("expected 3 assignments, got %d", len(assignments))
	}
	if assignments[0].Key != "go-city" || assignments[0].Value != "http://go-city:5202" {
		t.Fatalf("unexpected first assignment: %+v", assignments[0])
	}
	if assignments[2].Key != "/api/v1/cities" || assignments[2].Value != "go-city" {
		t.Fatalf("unexpected route assignment: %+v", assignments[2])
	}
}

func TestLoadConfigFromEnvRegistersUpstreamsAndRouteTargets(t *testing.T) {
	t.Setenv("GO_GATEWAY_UPSTREAMS", "go-config-service=http://go-config-service:5213")
	t.Setenv("GO_GATEWAY_ROUTE_TARGETS", "/api/v1/app/config=go-config-service")

	config := LoadConfigFromEnv()
	if config.ServiceURLs["go-config-service"] != "http://go-config-service:5213" {
		t.Fatalf("expected extra upstream to be registered, got %+v", config.ServiceURLs)
	}

	route, ok := matchRoute("/api/v1/app/config", config.Routes)
	if !ok {
		t.Fatal("expected config route match")
	}
	if route.ServiceName != "go-config-service" {
		t.Fatalf("expected route override, got %s", route.ServiceName)
	}
}
