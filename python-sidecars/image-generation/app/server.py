from __future__ import annotations

import json
import os
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

from app.models import GenerateCityImagesRequest, GenerateImageRequest
from app.service import ImageGenerationService, SidecarSettings, ValidationError


SERVICE = ImageGenerationService(SidecarSettings(
    public_base_url=os.getenv("IMAGE_SIDECAR_PUBLIC_BASE_URL", "https://storage.local"),
    max_city_concurrency=int(os.getenv("IMAGE_SIDECAR_MAX_CITY_CONCURRENCY", "3")),
))


class ImageSidecarHandler(BaseHTTPRequestHandler):
    server_version = "GoNomadsImageSidecar/0.1"

    def do_GET(self) -> None:
        parsed = urlparse(self.path)
        if parsed.path == "/health":
            self._write_json(HTTPStatus.OK, {"status": "healthy", "service": "image-generation-sidecar"})
            return

        prefix = "/internal/v1/images/tasks/"
        if parsed.path.startswith(prefix):
            task_id = parsed.path[len(prefix):]
            self._write_json(HTTPStatus.OK, SERVICE.get_task_status(task_id).to_dict())
            return

        self._write_json(HTTPStatus.NOT_FOUND, {"success": False, "message": "No route matched"})

    def do_POST(self) -> None:
        parsed = urlparse(self.path)
        try:
            payload = self._read_json()
            if parsed.path == "/internal/v1/images/generate":
                request = GenerateImageRequest.from_dict(payload)
                response = SERVICE.generate_image(request)
                self._write_json(HTTPStatus.OK, response.to_dict())
                return

            if parsed.path == "/internal/v1/images/city":
                request = GenerateCityImagesRequest.from_dict(payload)
                response = SERVICE.generate_city_images(request)
                self._write_json(HTTPStatus.OK if response.success else HTTPStatus.BAD_REQUEST, response.to_dict())
                return

            self._write_json(HTTPStatus.NOT_FOUND, {"success": False, "message": "No route matched"})
        except ValidationError as exc:
            self._write_json(HTTPStatus.BAD_REQUEST, {"success": False, "message": str(exc)})
        except json.JSONDecodeError:
            self._write_json(HTTPStatus.BAD_REQUEST, {"success": False, "message": "Invalid JSON body"})

    def log_message(self, format: str, *args: object) -> None:
        return

    def _read_json(self) -> dict[str, object]:
        length = int(self.headers.get("Content-Length") or "0")
        body = self.rfile.read(length) if length > 0 else b"{}"
        return json.loads(body.decode("utf-8"))

    def _write_json(self, status: HTTPStatus, payload: dict[str, object]) -> None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


def run() -> None:
    host = os.getenv("IMAGE_SIDECAR_HOST", "0.0.0.0")
    port = int(os.getenv("IMAGE_SIDECAR_PORT", "5222"))
    server = ThreadingHTTPServer((host, port), ImageSidecarHandler)
    server.serve_forever()


if __name__ == "__main__":
    run()
