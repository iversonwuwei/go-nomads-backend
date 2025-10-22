-- Active: 1760768405034@@db.lcfbajrocmjlqndkrsao.supabase.co@5432@postgres@public
-- UserService Database Schema for Supabase
-- This script creates the users and roles tables and related indexes

-- ============================================
-- Roles Table
-- ============================================

-- Create roles table
CREATE TABLE IF NOT EXISTS public.roles (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Insert default roles
INSERT INTO
    public.roles (id, name, description)
VALUES ('role_user', 'user', '普通用户角色'),
    (
        'role_admin',
        'admin',
        '管理员角色'
    )
ON CONFLICT (name) DO NOTHING;

-- ============================================
-- Users Table
-- ============================================

-- Create users table
CREATE TABLE IF NOT EXISTS public.users (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    phone VARCHAR(50),
password_hash VARCHAR(255),
role VARCHAR(50) DEFAULT 'user' NOT NULL,
role_id VARCHAR(50) DEFAULT 'role_user',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
CONSTRAINT fk_users_role_id FOREIGN KEY (role_id) REFERENCES public.roles (id) ON DELETE SET NULL
);

-- Create unique index on email
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

-- Create index on created_at for sorting
CREATE INDEX IF NOT EXISTS idx_users_created_at ON public.users(created_at DESC);

-- Create index on role for filtering
CREATE INDEX IF NOT EXISTS idx_users_role ON public.users (role);

-- Create index on role_id for foreign key lookups
CREATE INDEX IF NOT EXISTS idx_users_role_id ON public.users (role_id);

-- ============================================
-- Row Level Security
-- ============================================

-- Enable Row Level Security for roles table
ALTER TABLE public.roles ENABLE ROW LEVEL SECURITY;

-- Create policy for roles table (read-only for all users)
CREATE POLICY "Allow read access to roles" ON public.roles FOR
SELECT USING (true);
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

-- Create trigger to auto-update updated_at for users
DROP TRIGGER IF EXISTS update_users_updated_at ON public.users;
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON public.users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Create trigger to auto-update updated_at for roles
DROP TRIGGER IF EXISTS update_roles_updated_at ON public.roles;

CREATE TRIGGER update_roles_updated_at
    BEFORE UPDATE ON public.roles
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- Comments / Documentation
-- ============================================

COMMENT ON TABLE public.roles IS 'User roles definition table';

COMMENT ON COLUMN public.roles.id IS 'Unique role identifier';

COMMENT ON COLUMN public.roles.name IS 'Role name (unique)';

COMMENT ON COLUMN public.roles.description IS 'Role description';

COMMENT ON TABLE public.users IS 'User accounts for the application with authentication support';
COMMENT ON COLUMN public.users.id IS 'Unique user identifier';
COMMENT ON COLUMN public.users.name IS 'User full name';
COMMENT ON COLUMN public.users.email IS 'User email address (unique)';
COMMENT ON COLUMN public.users.phone IS 'User phone number (optional)';
COMMENT ON COLUMN public.users.password_hash IS 'BCrypt hashed password';

COMMENT ON COLUMN public.users.role IS 'User role string (deprecated - use role_id)';

COMMENT ON COLUMN public.users.role_id IS 'Foreign key reference to roles table';
COMMENT ON COLUMN public.users.created_at IS 'Timestamp when user was created';
COMMENT ON COLUMN public.users.updated_at IS 'Timestamp when user was last updated';
