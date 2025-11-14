#!/bin/bash

# Update walden.wuwei@gmail.com user role to admin
# This script connects to Supabase and updates the user role

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL_FILE="$SCRIPT_DIR/update-walden-to-admin.sql"

# Supabase connection details
SUPABASE_URL="https://lcfbajrocmjlqndkrsao.supabase.co"
SUPABASE_SERVICE_KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxjZmJhanJvY21qbHFuZGtyc2FvIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2MDcxODUyNSwiZXhwIjoyMDc2Mjk0NTI1fQ.H_rVUaL5hgwBRb2JH9cJwPRkwPZxMGy8Y-y3SX-a8yk"

echo "🔄 Updating user walden.wuwei@gmail.com role to admin..."
echo ""

# Read SQL file
SQL_CONTENT=$(cat "$SQL_FILE")

# Execute SQL via Supabase REST API
response=$(curl -s -X POST \
  "${SUPABASE_URL}/rest/v1/rpc/exec_sql" \
  -H "apikey: ${SUPABASE_SERVICE_KEY}" \
  -H "Authorization: Bearer ${SUPABASE_SERVICE_KEY}" \
  -H "Content-Type: application/json" \
  -d "{\"query\": $(echo "$SQL_CONTENT" | jq -Rs .)}")

echo "Response: $response"
echo ""

# Alternative: Use psql if available
if command -v psql &> /dev/null; then
    echo "Using psql to execute SQL..."
    
    # Supabase PostgreSQL connection string
    PGPASSWORD="bwTyaM1eJ1TRIZI3" psql \
        -h "db.lcfbajrocmjlqndkrsao.supabase.co" \
        -p 6543 \
        -U "postgres.lcfbajrocmjlqndkrsao" \
        -d "postgres" \
        -f "$SQL_FILE"
    
    if [ $? -eq 0 ]; then
        echo "✅ User role updated successfully!"
    else
        echo "❌ Failed to update user role"
        exit 1
    fi
else
    echo "⚠️  psql not found. Please install PostgreSQL client or run the SQL manually in Supabase Dashboard."
    echo ""
    echo "SQL to execute:"
    cat "$SQL_FILE"
fi
