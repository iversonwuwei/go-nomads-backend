#!/usr/bin/env python3
"""
用户城市内容表迁移脚本
使用 psycopg2 连接 Supabase PostgreSQL 并执行迁移
"""

import os

import psycopg2

# Supabase 连接信息
connection_params = {
    "host": "db.lcfbajrocmjlqndkrsao.supabase.co",
    "port": 6543,
    "database": "postgres",
    "user": "postgres.lcfbajrocmjlqndkrsao",
    "password": "bwTyaM1eJ1TRIZI3",
    "sslmode": "require"
}

# SQL 文件路径
sql_file_path = os.path.join(
    os.path.dirname(__file__),
    "create_user_city_content_tables.sql"
)

def run_migration():
    """执行数据库迁移"""
    print("🚀 开始执行用户城市内容表迁移...")
    print(f"   数据库: {connection_params['host']}:{connection_params['port']}/{connection_params['database']}")
    print(f"   SQL文件: {sql_file_path}")
    print("")
    
    try:
        # 读取 SQL 文件
        with open(sql_file_path, 'r', encoding='utf-8') as f:
            sql_script = f.read()
        
        print(f"✓ SQL 文件读取成功 ({len(sql_script)} 字符)")
        
        # 连接数据库
        conn = psycopg2.connect(**connection_params)
        conn.autocommit = True
        cursor = conn.cursor()
        
        print("✓ 数据库连接成功")
        print("")
        print("⏳ 执行 SQL 脚本...")
        
        # 执行 SQL 脚本
        cursor.execute(sql_script)
        
        print("")
        print("✅ 迁移成功完成!")
        print("")
        print("已创建的表：")
        print("  - user_city_photos (用户城市照片)")
        print("  - user_city_expenses (用户城市费用)")
        print("  - user_city_reviews (用户城市评论)")
        print("  - user_city_content_stats (统计视图)")
        print("")
        print("已配置：")
        print("  - RLS 行级安全策略")
        print("  - 性能优化索引")
        print("  - 外键约束")
        print("")
        
        # 验证表创建
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
              AND table_name LIKE 'user_city_%'
            ORDER BY table_name;
        """)
        
        tables = cursor.fetchall()
        print(f"✓ 验证: 找到 {len(tables)} 个表")
        for table in tables:
            print(f"  - {table[0]}")
        
        cursor.close()
        conn.close()
        
    except FileNotFoundError:
        print(f"❌ 错误: SQL 文件未找到: {sql_file_path}")
        return False
    except psycopg2.Error as e:
        print(f"❌ 数据库错误: {e}")
        return False
    except Exception as e:
        print(f"❌ 未知错误: {e}")
        return False
    
    return True

if __name__ == "__main__":
    success = run_migration()
    exit(0 if success else 1)
