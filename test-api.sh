#!/bin/bash

set -euo pipefail

echo "Testing EduShield API endpoints..."
echo "=================================="

BASE_URL="http://localhost:5153"

echo "1. Health checks"
echo -n "- GET /health: "
curl -sS "$BASE_URL/health" && echo
echo -n "- GET /api/health: "
curl -sS "$BASE_URL/api/health" && echo
echo -n "- GET /api/health/ping: "
curl -sS "$BASE_URL/api/health/ping" && echo
echo

echo "2. Students - list"
curl -sS "$BASE_URL/api/student" && echo
echo

echo "3. Students - create (unique email)"
EMAIL="student.$(date +%s)@example.com"
CREATE_BODY=$(cat <<JSON
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "$EMAIL",
  "phoneNumber": "123-456-7890",
  "dateOfBirth": "2000-01-01",
  "address": "123 Main St",
  "enrollmentDate": "2025-08-10",
  "gender": 1
}
JSON
)
echo "$CREATE_BODY"
CREATE_RESP=$(curl -sS -X POST -H "Content-Type: application/json" -d "$CREATE_BODY" "$BASE_URL/api/student")
echo "$CREATE_RESP"
ID=$(echo "$CREATE_RESP" | grep -oE '"id":"[0-9a-f-]+"' | cut -d '"' -f4 || true)
if [[ -z "${ID:-}" ]]; then
  echo "Failed to extract created ID" >&2
  exit 1
fi
echo "Created ID: $ID"
echo

echo "4. Students - get by id"
curl -sS "$BASE_URL/api/student/$ID" && echo
echo

echo "5. Students - update"
UPDATE_BODY=$(cat <<JSON
{
  "firstName": "John",
  "lastName": "Smith",
  "email": "$EMAIL",
  "phoneNumber": "222-333-4444",
  "dateOfBirth": "2000-01-01",
  "address": "456 Oak St",
  "enrollmentDate": "2025-08-10",
  "gender": 1
}
JSON
)
echo "$UPDATE_BODY"
curl -sS -o /dev/null -w "HTTP %{http_code}\n" -X PUT -H "Content-Type: application/json" -d "$UPDATE_BODY" "$BASE_URL/api/student/$ID"
echo "Verify after update:"
curl -sS "$BASE_URL/api/student/$ID" && echo
echo

echo "6. Students - delete"
curl -sS -o /dev/null -w "HTTP %{http_code}\n" -X DELETE "$BASE_URL/api/student/$ID"
echo -n "Verify deleted (expect 404): "
curl -sS -o /dev/null -w "%{http_code}\n" "$BASE_URL/api/student/$ID"
echo

echo "API testing completed!"
