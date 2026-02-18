#!/usr/bin/env python3
"""PipeRAG E2E API Test Suite - using urllib only"""
import urllib.request, urllib.error, json, random, string, io

BASE = "http://localhost:5000"
PASS = 0
FAIL = 0
results = []

def req(method, url, data=None, headers=None, expected=200):
    headers = headers or {}
    body = None
    if data is not None:
        if isinstance(data, dict):
            body = json.dumps(data).encode()
            headers["Content-Type"] = "application/json"
        else:
            body = data
    r = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        resp = urllib.request.urlopen(r, timeout=10)
        return resp.status, resp.read().decode()
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()
    except Exception as e:
        return 0, str(e)

def test(name, method, url, data=None, headers=None, expected=200):
    global PASS, FAIL
    status, body = req(method, url, data, headers)
    if status == expected:
        print(f"✅ PASS: {name} (HTTP {status})")
        PASS += 1
        results.append(f"✅ {name}")
    else:
        print(f"❌ FAIL: {name} (expected {expected}, got {status})")
        print(f"   Body: {body[:200]}")
        FAIL += 1
        results.append(f"❌ {name} (expected {expected}, got {status})")
    return status, body

def upload_file(url, filepath, headers, field="file", filename="test.txt"):
    boundary = "----PipeRAGBoundary" + ''.join(random.choices(string.ascii_letters, k=8))
    with open(filepath, "rb") as f:
        file_data = f.read()
    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="{field}"; filename="{filename}"\r\n'
        f"Content-Type: text/plain\r\n\r\n"
    ).encode() + file_data + f"\r\n--{boundary}--\r\n".encode()
    h = dict(headers)
    h["Content-Type"] = f"multipart/form-data; boundary={boundary}"
    return req("POST", url, data=body, headers=h)

print("=== PipeRAG E2E Tests ===\n")

# 1. Health
test("Health check", "GET", f"{BASE}/health")

# 2. Register
rand = ''.join(random.choices(string.ascii_lowercase, k=6))
email = f"e2e_{rand}@test.com"
s, b = test("Register", "POST", f"{BASE}/api/auth/register",
    {"email": email, "password": "Test1234!", "displayName": "E2E User"})
token = json.loads(b).get("accessToken", "") if s == 200 else ""

# 3. Login
s, b = test("Login", "POST", f"{BASE}/api/auth/login",
    {"email": email, "password": "Test1234!"})
if s == 200:
    token = json.loads(b).get("accessToken", "")

if not token:
    print("⚠️  No token")

auth = {"Authorization": f"Bearer {token}"}

# 4. User profile
test("Get user profile", "GET", f"{BASE}/api/users/me", headers=auth)

# 5. Create project
s, b = test("Create project", "POST", f"{BASE}/api/projects",
    {"name": "E2E Test Project", "description": "Test"}, headers=auth)
project_id = json.loads(b).get("id", "") if s == 200 else ""
if project_id:
    print(f"   Project ID: {project_id}")

# 6. List projects
test("List projects", "GET", f"{BASE}/api/projects", headers=auth)

# 7. Get project
if project_id:
    test("Get project by ID", "GET", f"{BASE}/api/projects/{project_id}", headers=auth)

# 8. Create pipeline
pipeline_id = ""
if project_id:
    s, b = test("Create pipeline", "POST", f"{BASE}/api/projects/{project_id}/pipelines",
        {"name": "Test Pipeline", "description": "E2E", "nodes": [], "edges": []}, headers=auth)
    if s == 200:
        pipeline_id = json.loads(b).get("id", "")
        print(f"   Pipeline ID: {pipeline_id}")

# 9. List pipelines
if project_id:
    test("List pipelines", "GET", f"{BASE}/api/projects/{project_id}/pipelines", headers=auth)

# 10. Upload document
if project_id:
    with open("/tmp/e2e_test.txt", "w") as f:
        f.write("Hello world test document for PipeRAG E2E testing.")
    s, b = upload_file(f"{BASE}/api/projects/{project_id}/documents", "/tmp/e2e_test.txt", auth)
    if s in (200, 201):
        print(f"✅ PASS: Upload document (HTTP {s})")
        PASS += 1; results.append("✅ Upload document")
    else:
        print(f"❌ FAIL: Upload document (got {s})")
        print(f"   Body: {b[:200]}")
        FAIL += 1; results.append(f"❌ Upload document ({s})")

# 11. List documents
if project_id:
    test("List documents", "GET", f"{BASE}/api/projects/{project_id}/documents", headers=auth)

# 12. Chat
if project_id:
    test("Chat query", "POST", f"{BASE}/api/projects/{project_id}/chat",
        {"message": "Hello", "query": "Hello"}, headers=auth)

# 13. Chat sessions
if project_id:
    test("List chat sessions", "GET", f"{BASE}/api/projects/{project_id}/chat/sessions", headers=auth)

# 14-15. Widget config
if project_id:
    test("Get widget config", "GET", f"{BASE}/api/projects/{project_id}/widget", headers=auth)
    test("Update widget config", "PUT", f"{BASE}/api/projects/{project_id}/widget",
        {"title": "Test Widget", "primaryColor": "#3B82F6", "position": "bottom-right"}, headers=auth)

# 16. Public widget config
if project_id:
    # may be 404 if not active
    s, b = test("Public widget config", "GET", f"{BASE}/api/widget/{project_id}/config")

# 17. embed.js
test("Widget embed.js", "GET", f"{BASE}/api/widget/embed.js")

# 18-19. Billing
test("Billing subscription", "GET", f"{BASE}/api/billing/subscription", headers=auth)
test("Billing usage", "GET", f"{BASE}/api/billing/usage", headers=auth)

# 20. Models
test("Get models", "GET", f"{BASE}/api/models", headers=auth)

print(f"\n{'='*40}")
print(f"RESULTS: {PASS} passed, {FAIL} failed")
print(f"{'='*40}")
for r in results:
    print(r)
