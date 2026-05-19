from __future__ import annotations

import hashlib
import threading
import time
import uuid
from dataclasses import dataclass

from app.models import (
    GeneratedImageInfo,
    GenerateCityImagesRequest,
    GenerateCityImagesResponse,
    GenerateImageRequest,
    GenerateImageResponse,
    ImageTaskStatusResponse,
)


class ValidationError(ValueError):
    pass


@dataclass(slots=True)
class SidecarSettings:
    public_base_url: str = "https://storage.local"
    max_city_concurrency: int = 3


class ImageGenerationService:
    def __init__(self, settings: SidecarSettings | None = None) -> None:
        self._settings = settings or SidecarSettings()
        self._city_semaphore = threading.BoundedSemaphore(self._settings.max_city_concurrency)
        self._tasks: dict[str, ImageTaskStatusResponse] = {}
        self._tasks_lock = threading.Lock()

    def generate_image(self, request: GenerateImageRequest) -> GenerateImageResponse:
        start = time.perf_counter()
        self._validate_image_request(request)

        task_id = uuid.uuid4().hex
        images = [self._build_image(request.bucket, request.path_prefix, request.size, index) for index in range(request.count)]
        duration_ms = int((time.perf_counter() - start) * 1000)

        self._set_task_status(ImageTaskStatusResponse(
            task_id=task_id,
            status="SUCCEEDED",
            image_urls=[image.url for image in images],
            succeeded_count=len(images),
            failed_count=0,
        ))

        return GenerateImageResponse(
            images=images,
            task_id=task_id,
            generation_time_ms=duration_ms,
            success=True,
        )

    def generate_city_images(self, request: GenerateCityImagesRequest) -> GenerateCityImagesResponse:
        start = time.perf_counter()
        self._validate_city_request(request)

        acquired = self._city_semaphore.acquire(timeout=120)
        if not acquired:
            return GenerateCityImagesResponse(
                city_id=request.city_id,
                portrait_image=None,
                landscape_images=[],
                generation_time_ms=int((time.perf_counter() - start) * 1000),
                success=False,
                error_message="城市图片生成并发槽位等待超时",
            )

        try:
            portrait = self._build_image(request.bucket, f"portrait/{request.city_id}", "720*1280", 0)
            landscapes = [
                self._build_image(request.bucket, f"landscape/{request.city_id}", "1280*720", index)
                for index in range(4)
            ]
            return GenerateCityImagesResponse(
                city_id=request.city_id,
                portrait_image=portrait,
                landscape_images=landscapes,
                generation_time_ms=int((time.perf_counter() - start) * 1000),
                success=True,
            )
        finally:
            self._city_semaphore.release()

    def get_task_status(self, task_id: str) -> ImageTaskStatusResponse:
        with self._tasks_lock:
            task = self._tasks.get(task_id)
        if task is None:
            return ImageTaskStatusResponse(task_id=task_id, status="UNKNOWN")
        return task

    def _set_task_status(self, status: ImageTaskStatusResponse) -> None:
        with self._tasks_lock:
            self._tasks[status.task_id] = status

    def _build_image(self, bucket: str, path_prefix: str | None, size: str, index: int) -> GeneratedImageInfo:
        stable_input = f"{bucket}:{path_prefix}:{size}:{index}:{time.time_ns()}"
        digest = hashlib.sha256(stable_input.encode("utf-8")).hexdigest()[:24]
        storage_prefix = path_prefix.strip("/") if path_prefix else "generated"
        storage_path = f"{storage_prefix}/00000000-0000-0000-0000-000000000000/{digest}.png"
        url = f"{self._settings.public_base_url.rstrip('/')}/{bucket}/{storage_path}"
        return GeneratedImageInfo(
            url=url,
            storage_path=storage_path,
            original_url=f"dry-run://wanx/{digest}",
            file_size=0,
        )

    @staticmethod
    def _validate_image_request(request: GenerateImageRequest) -> None:
        if not request.prompt.strip():
            raise ValidationError("prompt is required")
        if len(request.prompt) > 800:
            raise ValidationError("prompt must be 800 characters or fewer")
        if request.negative_prompt and len(request.negative_prompt) > 800:
            raise ValidationError("negativePrompt must be 800 characters or fewer")
        if request.count < 1 or request.count > 4:
            raise ValidationError("count must be between 1 and 4")
        if request.size not in {"1024*1024", "720*1280", "1280*720"}:
            raise ValidationError("size is not supported")

    @staticmethod
    def _validate_city_request(request: GenerateCityImagesRequest) -> None:
        if not request.city_id.strip():
            raise ValidationError("cityId is required")
        if not request.city_name.strip():
            raise ValidationError("cityName is required")
