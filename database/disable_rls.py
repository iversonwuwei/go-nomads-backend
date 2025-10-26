"""
禁用 coworking_spaces 表的 RLS (仅用于开发测试)
"""

from supabase import Client, create_client

# Supabase 配置
url = "https://lcfbajrocmjlqndkrsao.supabase.co"
# 需要使用 service_role key (从 Supabase Dashboard -> Settings -> API)
# 这里暂时使用 anon key,但无法执行 DDL
key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxjZmJhanJvY21qbHFuZGtyc2FvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjA3MTg1MjUsImV4cCI6MjA3NjI5NDUyNX0.-aYrl3f6AAhURF025S_4NwvehfugUiG3VR-wvZe3mRU"

supabase: Client = create_client(url, key)

# 执行 SQL
try:
    result = supabase.rpc('exec_sql', {
        'query': 'ALTER TABLE public.coworking_spaces DISABLE ROW LEVEL SECURITY;'
    }).execute()
    print("✅ RLS 已禁用")
    print(result)
except Exception as e:
    print(f"❌ 执行失败: {e}")
    print("\n⚠️  请手动在 Supabase Dashboard 的 SQL Editor 中执行以下 SQL:")
    print("\nALTER TABLE public.coworking_spaces DISABLE ROW LEVEL SECURITY;")
    print("\n或者访问: https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql/new")
