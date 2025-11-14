-- =====================================================
-- Storage RLS Policies for Storage Buckets
-- 修复图片上传 403 错误
-- =====================================================

-- 1. 确保所有存储桶存在

-- 1.1 avatars bucket (用户头像)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'avatars',
  'avatars',
  true,  -- 设为 public，允许公开访问
  5242880,  -- 5MB 文件大小限制
  ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg']
)
ON CONFLICT (id) DO UPDATE SET
  public = true,
  file_size_limit = 5242880,
  allowed_mime_types = ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg'];

-- 1.2 user-uploads bucket (用户其他上传内容)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'user-uploads',
  'user-uploads',
  true,  -- 设为 public，允许公开访问
  20971520,  -- 20MB 文件大小限制
  ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg']
)
ON CONFLICT (id) DO UPDATE SET
  public = true,
  file_size_limit = 20971520,
  allowed_mime_types = ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg'];

-- 1.3 city-photos bucket (城市照片)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'city-photos',
  'city-photos',
  true,
  20971520,  -- 20MB
  ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg']
)
ON CONFLICT (id) DO UPDATE SET
  public = true,
  file_size_limit = 20971520,
  allowed_mime_types = ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg'];

-- 1.4 coworking-photos bucket (共享办公空间照片)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'coworking-photos',
  'coworking-photos',
  true,
  20971520,  -- 20MB
  ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg']
)
ON CONFLICT (id) DO UPDATE SET
  public = true,
  file_size_limit = 20971520,
  allowed_mime_types = ARRAY['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/jpg'];

-- 2. 删除旧的策略（如果存在）
DROP POLICY IF EXISTS "Allow authenticated users to upload" ON storage.objects;
DROP POLICY IF EXISTS "Allow public read access" ON storage.objects;
DROP POLICY IF EXISTS "Allow users to update own files" ON storage.objects;
DROP POLICY IF EXISTS "Allow users to delete own files" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow authenticated upload" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow public read" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow user update own" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow user delete own" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow all authenticated upload" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow all authenticated update" ON storage.objects;
DROP POLICY IF EXISTS "Avatars: Allow all authenticated delete" ON storage.objects;
DROP POLICY IF EXISTS "Allow all upload" ON storage.objects;
DROP POLICY IF EXISTS "Allow all update" ON storage.objects;
DROP POLICY IF EXISTS "Allow all delete" ON storage.objects;

-- 3. 创建 RLS 策略

-- ==================== avatars bucket ====================
-- 策略1: 允许所有认证用户上传头像（因为使用外部JWT认证）
CREATE POLICY "Avatars: Allow all authenticated upload"
ON storage.objects
FOR INSERT
TO authenticated, anon  -- 同时允许 authenticated 和 anon
WITH CHECK (
  bucket_id = 'avatars'
);

-- 策略2: 允许公开读取所有头像
CREATE POLICY "Avatars: Allow public read"
ON storage.objects
FOR SELECT
TO public
USING (bucket_id = 'avatars');

-- 策略3: 允许所有认证用户更新头像
CREATE POLICY "Avatars: Allow all authenticated update"
ON storage.objects
FOR UPDATE
TO authenticated, anon
USING (bucket_id = 'avatars')
WITH CHECK (bucket_id = 'avatars');

-- 策略4: 允许所有认证用户删除头像
CREATE POLICY "Avatars: Allow all authenticated delete"
ON storage.objects
FOR DELETE
TO authenticated, anon
USING (bucket_id = 'avatars');

-- ==================== user-uploads bucket ====================
-- 策略5: 允许所有用户上传文件
CREATE POLICY "Allow all upload"
ON storage.objects
FOR INSERT
TO authenticated, anon
WITH CHECK (
  bucket_id IN ('user-uploads', 'city-photos', 'coworking-photos')
);

-- 策略6: 允许公开读取所有文件
CREATE POLICY "Allow public read access"
ON storage.objects
FOR SELECT
TO public
USING (bucket_id IN ('user-uploads', 'city-photos', 'coworking-photos', 'avatars'));

-- 策略7: 允许所有用户更新文件
CREATE POLICY "Allow all update"
ON storage.objects
FOR UPDATE
TO authenticated, anon
USING (bucket_id IN ('user-uploads', 'city-photos', 'coworking-photos'))
WITH CHECK (bucket_id IN ('user-uploads', 'city-photos', 'coworking-photos'));

-- 策略8: 允许所有用户删除文件
CREATE POLICY "Allow all delete"
ON storage.objects
FOR DELETE
TO authenticated, anon
USING (bucket_id IN ('user-uploads', 'city-photos', 'coworking-photos'));

-- 5. 验证策略
SELECT 
  schemaname,
  tablename,
  policyname,
  permissive,
  roles,
  cmd,
  qual,
  with_check
FROM pg_policies
WHERE tablename = 'objects'
AND schemaname = 'storage'
ORDER BY policyname;
