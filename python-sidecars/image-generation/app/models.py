from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class GeneratedImageInfo:
    url: str
    storage_path: str
    original_url: str
    file_size: int

    def to_dict(self) -> dict[str, Any]:
        return {
            "url": self.url,
            "storagePath": self.storage_path,
            "originalUrl": self.original_url,
            "fileSize": self.file_size,
        }


@dataclass(slots=True)
class GenerateImageRequest:
    prompt: str
    negative_prompt: str | None = None
    style: str = "<auto>"
    size: str = "1024*1024"
    count: int = 1
    bucket: str = "city-photos"
    path_prefix: str | None = None

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "GenerateImageRequest":
        return cls(
            prompt=str(payload.get("prompt") or ""),
            negative_prompt=payload.get("negativePrompt"),
            style=str(payload.get("style") or "<auto>"),
            size=str(payload.get("size") or "1024*1024"),
            count=int(payload.get("count") or 1),
            bucket=str(payload.get("bucket") or "city-photos"),
            path_prefix=payload.get("pathPrefix"),
        )


@dataclass(slots=True)
class GenerateImageResponse:
    images: list[GeneratedImageInfo]
    task_id: str
    generation_time_ms: int
    success: bool
    error_message: str | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "images": [image.to_dict() for image in self.images],
            "taskId": self.task_id,
            "generationTimeMs": self.generation_time_ms,
            "success": self.success,
            "errorMessage": self.error_message,
        }


@dataclass(slots=True)
class GenerateCityImagesRequest:
    city_id: str
    city_name: str
    country: str | None = None
    portrait_prompt: str | None = None
    landscape_prompt: str | None = None
    negative_prompt: str | None = None
    style: str = "<photography>"
    bucket: str = "city-photos"
    user_id: str | None = None

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "GenerateCityImagesRequest":
        return cls(
            city_id=str(payload.get("cityId") or ""),
            city_name=str(payload.get("cityName") or ""),
            country=payload.get("country"),
            portrait_prompt=payload.get("portraitPrompt"),
            landscape_prompt=payload.get("landscapePrompt"),
            negative_prompt=payload.get("negativePrompt"),
            style=str(payload.get("style") or "<photography>"),
            bucket=str(payload.get("bucket") or "city-photos"),
            user_id=payload.get("userId"),
        )


@dataclass(slots=True)
class GenerateCityImagesResponse:
    city_id: str
    portrait_image: GeneratedImageInfo | None
    landscape_images: list[GeneratedImageInfo]
    generation_time_ms: int
    success: bool
    error_message: str | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "cityId": self.city_id,
            "portraitImage": self.portrait_image.to_dict() if self.portrait_image else None,
            "landscapeImages": [image.to_dict() for image in self.landscape_images],
            "generationTimeMs": self.generation_time_ms,
            "success": self.success,
            "errorMessage": self.error_message,
        }


@dataclass(slots=True)
class ImageTaskStatusResponse:
    task_id: str
    status: str
    image_urls: list[str] = field(default_factory=list)
    succeeded_count: int = 0
    failed_count: int = 0
    error_message: str | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "taskId": self.task_id,
            "status": self.status,
            "imageUrls": self.image_urls,
            "succeededCount": self.succeeded_count,
            "failedCount": self.failed_count,
            "errorMessage": self.error_message,
        }
