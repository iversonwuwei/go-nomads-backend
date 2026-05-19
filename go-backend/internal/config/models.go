package config

import "time"

type APIResponse[T any] struct {
	Success bool     `json:"success"`
	Message string   `json:"message"`
	Data    *T       `json:"data,omitempty"`
	Errors  []string `json:"errors,omitempty"`
}

type AppConfig struct {
	Version        int                               `json:"version"`
	PublishedAt    *time.Time                        `json:"publishedAt"`
	StaticTexts    map[string]string                 `json:"staticTexts"`
	OptionGroups   map[string][]AppOptionItem        `json:"optionGroups"`
	SystemSettings map[string]map[string]SystemValue `json:"systemSettings"`
}

type AppOptionItem struct {
	Code    string  `json:"code"`
	Label   string  `json:"label"`
	LabelEn *string `json:"labelEn,omitempty"`
	Icon    *string `json:"icon,omitempty"`
	Color   *string `json:"color,omitempty"`
}

type SystemValue struct {
	Label        string  `json:"label"`
	ValueType    string  `json:"valueType"`
	Value        string  `json:"value"`
	DefaultValue *string `json:"defaultValue,omitempty"`
	Description  *string `json:"description,omitempty"`
}

type AppConfigVersion struct {
	Version     int        `json:"version"`
	PublishedAt *time.Time `json:"publishedAt"`
}

type publishedSnapshot struct {
	Version        int
	StaticTexts    []byte
	OptionGroups   []byte
	SystemSettings []byte
	PublishedAt    *time.Time
}
