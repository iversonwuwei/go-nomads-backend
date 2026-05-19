package product

import "time"

type APIResponse[T any] struct {
	Success bool     `json:"success"`
	Message string   `json:"message"`
	Data    *T       `json:"data,omitempty"`
	Errors  []string `json:"errors,omitempty"`
}

type PaginatedResponse[T any] struct {
	Items      []T `json:"items"`
	TotalCount int `json:"totalCount"`
	Page       int `json:"page"`
	PageSize   int `json:"pageSize"`
	TotalPages int `json:"totalPages"`
}

type Product struct {
	ID          string     `json:"id"`
	Name        string     `json:"name"`
	Description *string    `json:"description,omitempty"`
	Price       float64    `json:"price"`
	UserID      string     `json:"userId"`
	Category    *string    `json:"category,omitempty"`
	CreatedAt   *time.Time `json:"createdAt,omitempty"`
	UpdatedAt   *time.Time `json:"updatedAt,omitempty"`
}
