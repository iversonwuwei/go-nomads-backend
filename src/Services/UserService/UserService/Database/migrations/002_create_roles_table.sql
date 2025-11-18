-- Migration: Create roles table and establish relationship with users
-- Date: 2024-10-21
-- Description: Create a separate roles table with predefined roles and update users table to use foreign key

-- Create roles table
CREATE TABLE IF NOT EXISTS public.roles
(
    id          VARCHAR(50) PRIMARY KEY,
    name        VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    created_at  TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Insert default roles
INSERT INTO public.roles (id, name, description)
VALUES ('role_user', 'user', '普通用户角色'),
       ('role_admin', 'admin', '管理员角色')
ON CONFLICT
    (name)
    DO NOTHING;

-- Add role_id column to users table (if not exists)
ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS role_id VARCHAR (50);

-- Update existing users to use role_id reference
-- Map existing role string values to role_id
UPDATE public.users
SET role_id = CASE
                  WHEN role = 'admin' THEN 'role_admin'
                  ELSE 'role_user'
    END
WHERE role_id IS NULL;

-- Add foreign key constraint
ALTER TABLE public.users
    ADD CONSTRAINT fk_users_role_id
        FOREIGN KEY (role_id)
            REFERENCES public.roles (id)
            ON DELETE SET NULL;

-- Set default role for new users
ALTER TABLE public.users
    ALTER COLUMN role_id SET DEFAULT 'role_user';

-- Create index on role_id for better query performance
CREATE INDEX IF NOT EXISTS idx_users_role_id ON public.users(role_id);

-- Create trigger to auto-update updated_at for roles table
CREATE
OR
REPLACE FUNCTION update_roles_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = CURRENT_TIMESTAMP;
RETURN NEW;
END;
$$ language 'plpgsql';

DROP TRIGGER IF EXISTS update_roles_updated_at ON public.roles;
CREATE TRIGGER update_roles_updated_at
    BEFORE UPDATE
    ON public.roles
    FOR EACH ROW
    EXECUTE FUNCTION update_roles_updated_at();

-- Enable Row Level Security for roles table
ALTER TABLE public.roles ENABLE ROW LEVEL SECURITY;

-- Create policy for roles table (read-only for all users)
CREATE
POLICY "Allow read access to roles" ON public.roles
    FOR
SELECT
    USING
    (true);

-- Create policy for admin operations on roles (adjust based on your auth)
CREATE
POLICY "Allow admin operations on roles" ON public.roles
    FOR ALL
    USING (true)
    WITH
CHECK (true);

-- Add comments
COMMENT ON TABLE public.roles IS 'User roles definition table';
COMMENT ON COLUMN public.roles.id IS 'Unique role identifier';
COMMENT ON COLUMN public.roles.name IS 'Role name (unique)';
COMMENT ON COLUMN public.roles.description IS 'Role description';
COMMENT ON COLUMN public.users.role_id IS 'Foreign key reference to roles table';

-- Optional: You may want to drop the old role column after confirming everything works
-- WARNING: Only run this after verifying the migration is successful
-- ALTER TABLE public.users DROP COLUMN IF EXISTS role;
