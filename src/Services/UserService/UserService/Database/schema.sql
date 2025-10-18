-- Active: 1760768405034@@db.lcfbajrocmjlqndkrsao.supabase.co@5432@postgres@public
-- UserService Database Schema for Supabase
-- This script creates the users table and related indexes

-- Create users table
CREATE TABLE IF NOT EXISTS public.users (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    phone VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create unique index on email
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

-- Create index on created_at for sorting
CREATE INDEX IF NOT EXISTS idx_users_created_at ON public.users(created_at DESC);

-- Enable Row Level Security (RLS)
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- Create policy to allow all operations (adjust based on your auth requirements)
CREATE POLICY "Allow all operations on users" ON public.users
    FOR ALL
    USING (true)
    WITH CHECK (true);

-- Insert sample data (optional)
INSERT INTO public.users (id, name, email, phone, created_at, updated_at)
VALUES 
    ('1', 'John Doe', 'john@example.com', '123-456-7890', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('2', 'Jane Smith', 'jane@example.com', '098-765-4321', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- Create function to automatically update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger to auto-update updated_at
DROP TRIGGER IF EXISTS update_users_updated_at ON public.users;
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON public.users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

COMMENT ON TABLE public.users IS 'User accounts for the application';
COMMENT ON COLUMN public.users.id IS 'Unique user identifier';
COMMENT ON COLUMN public.users.name IS 'User full name';
COMMENT ON COLUMN public.users.email IS 'User email address (unique)';
COMMENT ON COLUMN public.users.phone IS 'User phone number (optional)';
COMMENT ON COLUMN public.users.created_at IS 'Timestamp when user was created';
COMMENT ON COLUMN public.users.updated_at IS 'Timestamp when user was last updated';
