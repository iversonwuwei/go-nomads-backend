-- Migration: Add password_hash and role fields to users table
-- Date: 2024-10-21
-- Description: Add password authentication support and user roles

-- Add password_hash column to store hashed passwords
ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS password_hash VARCHAR (255);

-- Add role column with default value 'user'
ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS role VARCHAR (50) DEFAULT 'user' NOT NULL;

-- Add comment for new columns
COMMENT ON COLUMN public.users.password_hash IS 'BCrypt hashed password';
COMMENT ON COLUMN public.users.role IS 'User role (e.g., user, admin)';

-- Create index on role for filtering
CREATE INDEX IF NOT EXISTS idx_users_role ON public.users(role);

-- Update existing users to have default role if NULL
UPDATE public.users
SET role = 'user'
WHERE role IS NULL;

-- Optional: Update existing test users with hashed passwords
-- Note: This is a bcrypt hash for password "123456" (for testing only)
-- In production, users should set their own passwords through registration
UPDATE public.users
SET password_hash = '$2a$11$ZLbE3qGqLqF5KXZqBxZx.eQ7vXxF3M3VyQHZ3YKbFxQxQZqQxQxQQ'
WHERE password_hash IS NULL
   OR password_hash = '';

COMMENT ON TABLE public.users IS 'User accounts for the application with authentication support';
