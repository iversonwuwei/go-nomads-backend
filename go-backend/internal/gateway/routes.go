package gateway

import "strings"

func defaultRoutes() []Route {
	var routes []Route
	add := func(service string, prefixes ...string) {
		for _, prefix := range prefixes {
			routes = append(routes, Route{ServiceName: service, Prefix: prefix, Order: len(routes) + 1})
		}
	}

	add("city-service",
		"/api/v1/admin/city-reviews",
		"/api/v1/admin/pros-cons",
		"/api/v1/admin/moderators",
		"/api/v1/admin/moderator-applications",
		"/api/v1/user-favorite-cities",
		"/api/v1/user-content/pros-cons",
		"/api/v1/cities",
		"/api/v1/countries",
		"/api/v1/provinces",
	)
	add("cache-service", "/api/v1/cache")
	add("user-service",
		"/api/v1/admin/membership",
		"/api/v1/admin/legal",
		"/api/v1/admin/audit/events",
		"/api/v1/auth",
		"/api/v1/reports",
		"/api/v1/users",
		"/api/v1/travel-history",
		"/api/v1/visited-places",
		"/api/v1/membership",
		"/api/v1/payments",
		"/api/v1/roles",
		"/api/v1/skills",
		"/api/v1/interests",
		"/api/v1/profile-snapshot",
	)
	add("event-service", "/api/v1/event-types", "/api/v1/events", "/hubs/meetup")
	add("ai-image-service", "/api/v1/ai/images")
	add("ai-service",
		"/api/v1/admin/travel-plans",
		"/api/v1/admin/community",
		"/api/v1/admin/ai",
		"/api/v1/ai",
		"/api/v1/migration-workspace",
		"/api/v1/explore-dashboard",
		"/api/v1/land-hub",
		"/api/v1/community-snapshot",
		"/api/v1/community",
		"/api/v1/budgets",
		"/api/v1/visa",
	)
	add("coworking-service", "/api/v1/coworking", "/api/v1/coworking-spaces")
	add("search-service", "/api/v1/search", "/api/v1/index")
	add("accommodation-service", "/api/v1/admin/hotel-reviews", "/api/v1/hotels")
	add("product-service", "/api/v1/products")
	add("message-service",
		"/api/v1/admin/notifications",
		"/api/v1/admin/chats",
		"/api/v1/im",
		"/api/v1/notifications",
		"/api/v1/chats",
		"/hubs/chat",
		"/hubs/notifications",
		"/hubs/ai-progress",
		"/api/v1/inbox",
	)
	add("innovation-service", "/api/innovations", "/api/v1/innovations", "/api/v1/innovation-projects")
	add("config-service", "/api/v1/admin/static-texts", "/api/v1/admin/option-groups", "/api/v1/admin/config", "/api/v1/app/config")

	return routes
}

func matchRoute(path string, routes []Route) (Route, bool) {
	var matched Route
	found := false

	for _, route := range routes {
		if pathMatchesPrefix(path, route.Prefix) {
			if !found || len(route.Prefix) > len(matched.Prefix) || (len(route.Prefix) == len(matched.Prefix) && route.Order < matched.Order) {
				matched = route
				found = true
			}
		}
	}

	return matched, found
}

func pathMatchesPrefix(path string, prefix string) bool {
	path = strings.TrimRight(path, "/")
	prefix = strings.TrimRight(prefix, "/")

	if path == prefix {
		return true
	}

	return strings.HasPrefix(path, prefix+"/")
}

func isPublicPath(path string, method string, publicPaths []string, publicGetPaths []string) bool {
	for _, prefix := range publicPaths {
		if pathMatchesPrefix(path, prefix) {
			return true
		}
	}

	if strings.EqualFold(method, "GET") {
		for _, prefix := range publicGetPaths {
			if pathMatchesPrefix(path, prefix) {
				return true
			}
		}
	}

	return false
}
