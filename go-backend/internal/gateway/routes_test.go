package gateway

import "testing"

func TestMatchRouteUsesLongestPrefix(t *testing.T) {
	routes := []Route{
		{ServiceName: "city-service", Prefix: "/api/v1/cities", Order: 1},
		{ServiceName: "user-service", Prefix: "/api/v1/cities/moderator", Order: 2},
	}

	route, ok := matchRoute("/api/v1/cities/moderator/apply", routes)
	if !ok {
		t.Fatal("expected route match")
	}
	if route.ServiceName != "user-service" {
		t.Fatalf("expected longest prefix route, got %s", route.ServiceName)
	}
}

func TestPublicGetPathOnlyAppliesToGet(t *testing.T) {
	if !isPublicPath("/api/v1/cities/search", "GET", nil, []string{"/api/v1/cities"}) {
		t.Fatal("expected city GET to be public")
	}
	if isPublicPath("/api/v1/cities", "POST", nil, []string{"/api/v1/cities"}) {
		t.Fatal("expected city POST to require auth")
	}
}

func TestAIImageRouteCanUseDedicatedService(t *testing.T) {
	route, ok := matchRoute("/api/v1/ai/images/city/async", defaultRoutes())
	if !ok {
		t.Fatal("expected route match")
	}
	if route.ServiceName != "ai-image-service" {
		t.Fatalf("expected ai image route, got %s", route.ServiceName)
	}
}

func TestRouteTargetOverrideReplacesDefaultPrefix(t *testing.T) {
	routes := applyRouteTargets(defaultRoutes(), []assignment{{Key: "/api/v1/cities", Value: "go-city-service"}})

	route, ok := matchRoute("/api/v1/cities/search", routes)
	if !ok {
		t.Fatal("expected route match")
	}
	if route.ServiceName != "go-city-service" {
		t.Fatalf("expected go-city-service route, got %s", route.ServiceName)
	}
}

func TestRouteTargetOverrideCanAddMoreSpecificCanary(t *testing.T) {
	routes := applyRouteTargets(defaultRoutes(), []assignment{{Key: "/api/v1/cities/canary", Value: "go-city-service"}})

	route, ok := matchRoute("/api/v1/cities/canary/list", routes)
	if !ok {
		t.Fatal("expected route match")
	}
	if route.ServiceName != "go-city-service" {
		t.Fatalf("expected canary route, got %s", route.ServiceName)
	}

	defaultRoute, ok := matchRoute("/api/v1/cities/search", routes)
	if !ok {
		t.Fatal("expected default route match")
	}
	if defaultRoute.ServiceName != "city-service" {
		t.Fatalf("expected default city route to remain, got %s", defaultRoute.ServiceName)
	}
}
