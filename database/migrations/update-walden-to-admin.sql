-- Update walden.wuwei@gmail.com user role to admin
-- Date: 2025-11-14
-- Description: Change user role from 'user' to 'admin' for walden.wuwei@gmail.com

-- Update user role_id to admin
UPDATE public.users 
SET 
    role_id = 'role_admin',
    role = 'admin',
    updated_at = CURRENT_TIMESTAMP
WHERE email = 'walden.wuwei@gmail.com';

-- Verify the update
SELECT 
    id,
    name,
    email,
    role,
    role_id,
    updated_at
FROM public.users
WHERE email = 'walden.wuwei@gmail.com';
