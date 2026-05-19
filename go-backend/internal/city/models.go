package city

type ApiResponse[T any] struct {
	Success bool     `json:"success"`
	Message string   `json:"message,omitempty"`
	Data    *T       `json:"data,omitempty"`
	Errors  []string `json:"errors,omitempty"`
}

type CityRegionTab struct {
	Key          string `json:"key"`
	Label        string `json:"label"`
	CityCount    int    `json:"cityCount"`
	DisplayOrder int    `json:"displayOrder"`
}

type regionSource struct {
	Region    *string
	Continent *string
}

var regionDisplayOrders = map[string]int{
	"Asia":          1,
	"Europe":        2,
	"North America": 3,
	"South America": 4,
	"Oceania":       5,
	"Africa":        6,
	"Middle East":   7,
	"Other":         998,
}
