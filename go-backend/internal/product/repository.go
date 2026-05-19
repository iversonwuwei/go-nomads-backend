package product

import "sort"

type Repository interface {
	List() []Product
	GetByID(id string) (Product, bool)
	GetByUserID(userID string) []Product
}

type MemoryRepository struct {
	products []Product
}

func NewMemoryRepository() *MemoryRepository {
	return &MemoryRepository{products: sampleProducts()}
}

func (repository *MemoryRepository) List() []Product {
	items := append([]Product(nil), repository.products...)
	sort.SliceStable(items, func(left int, right int) bool {
		return items[left].ID < items[right].ID
	})
	return items
}

func (repository *MemoryRepository) GetByID(id string) (Product, bool) {
	for _, product := range repository.products {
		if product.ID == id {
			return product, true
		}
	}
	return Product{}, false
}

func (repository *MemoryRepository) GetByUserID(userID string) []Product {
	items := make([]Product, 0)
	for _, product := range repository.products {
		if product.UserID == userID {
			items = append(items, product)
		}
	}
	sort.SliceStable(items, func(left int, right int) bool {
		return items[left].ID < items[right].ID
	})
	return items
}

func sampleProducts() []Product {
	laptopDescription := "High-performance laptop"
	laptopCategory := "Electronics"
	laptopCreatedAt := mustTime("2026-05-07T08:00:00Z")
	laptopUpdatedAt := mustTime("2026-05-07T08:00:00Z")

	mugDescription := "Ceramic coffee mug"
	mugCategory := "Home & Kitchen"
	mugCreatedAt := mustTime("2026-05-07T08:05:00Z")
	mugUpdatedAt := mustTime("2026-05-07T08:05:00Z")

	return []Product{
		{
			ID:          "1",
			Name:        "Laptop",
			Description: &laptopDescription,
			Price:       999.99,
			UserID:      "1",
			Category:    &laptopCategory,
			CreatedAt:   &laptopCreatedAt,
			UpdatedAt:   &laptopUpdatedAt,
		},
		{
			ID:          "2",
			Name:        "Coffee Mug",
			Description: &mugDescription,
			Price:       15.99,
			UserID:      "2",
			Category:    &mugCategory,
			CreatedAt:   &mugCreatedAt,
			UpdatedAt:   &mugUpdatedAt,
		},
	}
}
