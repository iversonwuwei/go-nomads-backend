import unittest

from app.models import GenerateCityImagesRequest, GenerateImageRequest
from app.service import ImageGenerationService, ValidationError


class ImageGenerationServiceTests(unittest.TestCase):
    def test_generate_image_returns_compatible_shape(self) -> None:
        service = ImageGenerationService()

        response = service.generate_image(GenerateImageRequest(prompt="Bangkok skyline", count=2))

        self.assertTrue(response.success)
        self.assertEqual(len(response.images), 2)
        payload = response.to_dict()
        self.assertIn("taskId", payload)
        self.assertIn("generationTimeMs", payload)
        self.assertEqual(payload["images"][0]["fileSize"], 0)

    def test_generate_city_images_returns_portrait_and_landscapes(self) -> None:
        service = ImageGenerationService()

        response = service.generate_city_images(GenerateCityImagesRequest(city_id="city-1", city_name="Bangkok"))

        self.assertTrue(response.success)
        self.assertIsNotNone(response.portrait_image)
        self.assertEqual(len(response.landscape_images), 4)
        payload = response.to_dict()
        self.assertEqual(payload["cityId"], "city-1")
        self.assertIn("portrait/city-1", payload["portraitImage"]["storagePath"])

    def test_rejects_invalid_count(self) -> None:
        service = ImageGenerationService()

        with self.assertRaises(ValidationError):
            service.generate_image(GenerateImageRequest(prompt="x", count=5))


if __name__ == "__main__":
    unittest.main()
