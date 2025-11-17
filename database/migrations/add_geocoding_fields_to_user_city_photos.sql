-- Adds geocoding metadata columns required by the city photo batch submission API.
-- Safe to run multiple times thanks to IF NOT EXISTS guards.

ALTER TABLE user_city_photos
    ADD COLUMN IF NOT EXISTS description TEXT,
    ADD COLUMN IF NOT EXISTS place_name TEXT,
    ADD COLUMN IF NOT EXISTS address TEXT,
    ADD COLUMN IF NOT EXISTS latitude DOUBLE PRECISION,
    ADD COLUMN IF NOT EXISTS longitude DOUBLE PRECISION;
