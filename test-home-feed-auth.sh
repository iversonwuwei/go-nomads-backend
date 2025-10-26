#!/bin/bash

echo "Testing Home Feed API with Authentication"

# Step 1: Login
echo "1. Login..."
LOGIN_RESPONSE=$(curl -s -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "walden.wuwei@gmail.com", "password": "walden123456"}')

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')
USER_ID=$(echo $LOGIN_RESPONSE | jq -r '.data.user.id')

echo "Token: $TOKEN"
echo "User ID: $USER_ID"
echo ""

# Step 2: Call Home Feed API with Auth
echo "2. Calling Home Feed API WITH Authorization..."
curl -s -X GET "http://localhost:5000/api/v1/home/feed?cityLimit=5&meetupLimit=10" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-User-Id: $USER_ID" | jq '.data.meetups[] | {title, isParticipant}'
echo ""

# Step 3: Call Home Feed API without Auth (for comparison)
echo "3. Calling Home Feed API WITHOUT Authorization..."
curl -s -X GET "http://localhost:5000/api/v1/home/feed?cityLimit=5&meetupLimit=10" | jq '.data.meetups[] | {title, isParticipant}'

