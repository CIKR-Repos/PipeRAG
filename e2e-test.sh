#!/bin/bash
# PipeRAG E2E Test Script
set -e

BASE=http://localhost:5000
PASS=0
FAIL=0
RESULTS=""

test_endpoint() {
  local name="$1" method="$2" url="$3" data="$4" headers="$5" expected_status="$6"
  
  cmd="curl -s -o /tmp/e2e_body -w '%{http_code}' -X $method"
  if [ -n "$headers" ]; then
    for h in $headers; do cmd="$cmd -H '$h'"; done
  fi
  if [ -n "$data" ]; then
    cmd="$cmd -H 'Content-Type: application/json' -d '$data'"
  fi
  cmd="$cmd '$url'"
  
  status=$(eval $cmd)
  body=$(cat /tmp/e2e_body)
  
  if [ "$status" = "$expected_status" ]; then
    echo "✅ PASS: $name (HTTP $status)"
    PASS=$((PASS+1))
    RESULTS="$RESULTS\n✅ $name"
  else
    echo "❌ FAIL: $name (expected $expected_status, got $status)"
    echo "   Body: $(echo $body | head -c 200)"
    FAIL=$((FAIL+1))
    RESULTS="$RESULTS\n❌ $name (expected $expected_status, got $status)"
  fi
  echo "$body" > /tmp/e2e_last_body
}

echo "=== PipeRAG E2E Tests ==="
echo ""

# 1. Health
test_endpoint "Health check" GET "$BASE/health" "" "" "200"

# 2. Register
RAND=$RANDOM
test_endpoint "Register new user" POST "$BASE/api/auth/register" \
  "{\"email\":\"e2e_${RAND}@test.com\",\"password\":\"Test1234!\",\"displayName\":\"E2E User\"}" "" "200"
TOKEN=$(cat /tmp/e2e_last_body | python3 -c "import sys,json; print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null || echo "")

# 3. Login
test_endpoint "Login" POST "$BASE/api/auth/login" \
  "{\"email\":\"e2e_${RAND}@test.com\",\"password\":\"Test1234!\"}" "" "200"
TOKEN=$(cat /tmp/e2e_last_body | python3 -c "import sys,json; print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null || echo "")

if [ -z "$TOKEN" ]; then
  echo "⚠️  No token obtained, remaining tests may fail"
fi

AUTH="Authorization: Bearer $TOKEN"

# 4. Get user profile
test_endpoint "Get user profile" GET "$BASE/api/users/me" "" "$AUTH" "200"

# 5. Create project
test_endpoint "Create project" POST "$BASE/api/projects" \
  "{\"name\":\"E2E Test Project\",\"description\":\"Test\"}" "$AUTH" "200"
PROJECT_ID=$(cat /tmp/e2e_last_body | python3 -c "import sys,json; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || echo "")

if [ -z "$PROJECT_ID" ]; then
  # try 201
  PROJECT_ID=$(cat /tmp/e2e_last_body | python3 -c "import sys,json; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || echo "")
fi
echo "   Project ID: $PROJECT_ID"

# 6. List projects
test_endpoint "List projects" GET "$BASE/api/projects" "" "$AUTH" "200"

# 7. Get project by ID
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "Get project by ID" GET "$BASE/api/projects/$PROJECT_ID" "" "$AUTH" "200"
fi

# 8. Create pipeline
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "Create pipeline" POST "$BASE/api/projects/$PROJECT_ID/pipelines" \
    "{\"name\":\"Test Pipeline\",\"description\":\"E2E test pipeline\",\"nodes\":[],\"edges\":[]}" "$AUTH" "200"
  PIPELINE_ID=$(cat /tmp/e2e_last_body | python3 -c "import sys,json; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || echo "")
  echo "   Pipeline ID: $PIPELINE_ID"
fi

# 9. List pipelines
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "List pipelines" GET "$BASE/api/projects/$PROJECT_ID/pipelines" "" "$AUTH" "200"
fi

# 10. Upload document (text file)
if [ -n "$PROJECT_ID" ]; then
  echo "Hello world test document for PipeRAG E2E testing." > /tmp/e2e_test.txt
  status=$(curl -s -o /tmp/e2e_body -w '%{http_code}' -X POST \
    -H "$AUTH" \
    -F "file=@/tmp/e2e_test.txt" \
    "$BASE/api/projects/$PROJECT_ID/documents")
  body=$(cat /tmp/e2e_body)
  if [ "$status" = "200" ] || [ "$status" = "201" ]; then
    echo "✅ PASS: Upload document (HTTP $status)"
    PASS=$((PASS+1))
    RESULTS="$RESULTS\n✅ Upload document"
  else
    echo "❌ FAIL: Upload document (got $status)"
    echo "   Body: $(echo $body | head -c 200)"
    FAIL=$((FAIL+1))
    RESULTS="$RESULTS\n❌ Upload document (got $status)"
  fi
  DOC_ID=$(echo $body | python3 -c "import sys,json; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || echo "")
fi

# 11. List documents
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "List documents" GET "$BASE/api/projects/$PROJECT_ID/documents" "" "$AUTH" "200"
fi

# 12. Chat
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "Chat query" POST "$BASE/api/projects/$PROJECT_ID/chat" \
    "{\"message\":\"Hello, what is this about?\",\"query\":\"Hello\"}" "$AUTH" "200"
fi

# 13. Chat sessions
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "List chat sessions" GET "$BASE/api/projects/$PROJECT_ID/chat/sessions" "" "$AUTH" "200"
fi

# 14. Widget config GET
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "Get widget config" GET "$BASE/api/projects/$PROJECT_ID/widget" "" "$AUTH" "200"
fi

# 15. Widget config PUT
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "Update widget config" PUT "$BASE/api/projects/$PROJECT_ID/widget" \
    "{\"title\":\"Test Widget\",\"primaryColor\":\"#3B82F6\",\"position\":\"bottom-right\"}" "$AUTH" "200"
fi

# 16. Public widget config
if [ -n "$PROJECT_ID" ]; then
  test_endpoint "Public widget config" GET "$BASE/api/widget/$PROJECT_ID/config" "" "" "200"
fi

# 17. Widget embed.js
test_endpoint "Widget embed.js" GET "$BASE/api/widget/embed.js" "" "" "200"

# 18. Billing subscription
test_endpoint "Billing subscription" GET "$BASE/api/billing/subscription" "" "$AUTH" "200"

# 19. Billing usage
test_endpoint "Billing usage" GET "$BASE/api/billing/usage" "" "$AUTH" "200"

# 20. Get models
test_endpoint "Get models" GET "$BASE/api/models" "" "$AUTH" "200"

echo ""
echo "=================================="
echo "RESULTS: $PASS passed, $FAIL failed"
echo "=================================="
echo -e "$RESULTS"
