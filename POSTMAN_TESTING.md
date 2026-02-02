# Postman Testing Guide

Complete testing guide for Fee Management Service API using Postman.

## Prerequisites

1. **Postman** installed ([Download](https://www.postman.com/downloads/))
2. **JWT Token** - See [JWT_TESTING.md](./JWT_TESTING.md) for token generation
3. **Running API** - Application should be running on `https://localhost:5001`

## Postman Collection Setup

### 1. Create New Collection

1. Open Postman
2. Click **New** → **Collection**
3. Name it: `Fee Management Service`

### 2. Configure Collection Variables

1. Select the collection
2. Go to **Variables** tab
3. Add the following variables:

| Variable | Initial Value | Current Value |
|----------|---------------|---------------|
| `baseUrl` | `https://localhost:5001` | `https://localhost:5001` |
| `jwtToken` | `your-jwt-token-here` | `your-jwt-token-here` |
| `schoolId` | `550e8400-e29b-41d4-a716-446655440000` | `550e8400-e29b-41d4-a716-446655440000` |
| `userId` | `user-123` | `user-123` |
| `feeId` | `fee-123` | `fee-123` |

### 3. Configure Collection Authorization

1. Go to **Authorization** tab
2. Type: **Bearer Token**
3. Token: `{{jwtToken}}`

This will automatically add the Authorization header to all requests.

## API Endpoints

### 1. Generate Presigned URL

**Request:**
- **Method:** `POST`
- **URL:** `{{baseUrl}}/api/fees/presigned-url`
- **Headers:**
  - `Content-Type: application/json`
  - `Authorization: Bearer {{jwtToken}}`
- **Body (raw JSON):**
```json
{
  "feeId": "{{feeId}}",
  "fileName": "test-image.jpg",
  "contentType": "image/jpeg",
  "fileSize": 102400
}
```

**Expected Response:** `200 OK`
```json
{
  "presignedUrl": "https://bucket.s3.region.amazonaws.com/...?X-Amz-Signature=...",
  "s3Key": "schools/{{schoolId}}/fees/{{feeId}}/test-image.jpg",
  "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{{schoolId}}/fees/{{feeId}}/test-image.jpg",
  "expiresAt": "2026-02-01T15:30:00Z"
}
```

**Test Script (add to Tests tab):**
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has presignedUrl", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('presignedUrl');
    pm.expect(jsonData.presignedUrl).to.include('amazonaws.com');
});

// Save imageUrl for next request
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.collectionVariables.set("imageUrl", jsonData.imageUrl);
    pm.collectionVariables.set("presignedUrl", jsonData.presignedUrl);
}
```

### 2. Upload Image to S3 (Using Presigned URL)

**Request:**
- **Method:** `PUT`
- **URL:** `{{presignedUrl}}` (from previous response)
- **Headers:**
  - `Content-Type: image/jpeg`
- **Body:** 
  - Select **binary**
  - Choose an image file (JPG, PNG, or WebP, max 5MB)

**Expected Response:** `200 OK` (from S3)

**Note:** This request goes directly to S3, not to your API server.

### 3. Create Fee

**Request:**
- **Method:** `POST`
- **URL:** `{{baseUrl}}/api/fees`
- **Headers:**
  - `Content-Type: application/json`
  - `Authorization: Bearer {{jwtToken}}`
- **Body (raw JSON):**
```json
{
  "title": "Class Fee - Semester 1",
  "description": "Fee for first semester classes including all subjects",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "{{imageUrl}}"
}
```

**Expected Response:** `201 Created`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "schoolId": "{{schoolId}}",
  "title": "Class Fee - Semester 1",
  "description": "Fee for first semester classes including all subjects",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "{{imageUrl}}",
  "status": "Active",
  "createdBy": "{{userId}}",
  "createdAt": "2026-02-01T14:30:00Z"
}
```

**Test Script:**
```javascript
pm.test("Status code is 201", function () {
    pm.response.to.have.status(201);
});

pm.test("Response has fee ID", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('id');
    pm.expect(jsonData.id).to.be.a('string');
});

pm.test("Fee has correct schoolId", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.schoolId).to.eql(pm.collectionVariables.get("schoolId"));
});
```

## Test Cases

### Success Cases

#### Test Case 1: Get Presigned URL with Valid File Metadata
- ✅ Use valid file metadata (fileName, contentType, fileSize < 5MB)
- ✅ Expected: 200 OK with presignedUrl

#### Test Case 2: Upload Image to S3 Using Presigned URL
- ✅ Use presignedUrl from previous response
- ✅ Upload valid image file (JPG, PNG, or WebP)
- ✅ Expected: 200 OK from S3

#### Test Case 3: Create Fee with Valid ImageUrl
- ✅ Use imageUrl from presigned URL response
- ✅ Include all required fields
- ✅ Expected: 201 Created with FeeResponse

#### Test Case 4: Create Fee without Image
- ✅ Omit imageUrl field
- ✅ Include all other required fields
- ✅ Expected: 201 Created with FeeResponse (imageUrl = null)

### Error Cases

#### Test Case 5: Missing Required Fields
**Request:**
```json
{
  "title": "",
  "amount": 1500.00
}
```
- ❌ Expected: 400 Bad Request with validation errors

#### Test Case 6: Invalid Amount (Negative or Zero)
**Request:**
```json
{
  "title": "Test Fee",
  "amount": -100,
  "feeType": "ClassFee"
}
```
- ❌ Expected: 400 Bad Request

#### Test Case 7: File Too Large (>5MB) in Presigned URL Request
**Request:**
```json
{
  "feeId": "fee-123",
  "fileName": "large-image.jpg",
  "contentType": "image/jpeg",
  "fileSize": 6000000
}
```
- ❌ Expected: 400 Bad Request

#### Test Case 8: Invalid Content Type in Presigned URL Request
**Request:**
```json
{
  "feeId": "fee-123",
  "fileName": "document.pdf",
  "contentType": "application/pdf",
  "fileSize": 102400
}
```
- ❌ Expected: 400 Bad Request

#### Test Case 9: Invalid ImageUrl Format in Create Fee Request
**Request:**
```json
{
  "title": "Test Fee",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "https://invalid-url.com/image.jpg"
}
```
- ❌ Expected: 400 Bad Request

#### Test Case 10: Unauthorized (No Token)
**Request:**
- Remove Authorization header
- ❌ Expected: 401 Unauthorized

#### Test Case 11: Forbidden (Wrong Role)
**Request:**
- Use JWT token with Role = "Student" (instead of "SchoolAdmin")
- ❌ Expected: 403 Forbidden

#### Test Case 12: Expired Presigned URL
**Request:**
- Wait 10+ minutes after generating presigned URL
- Try to upload to S3
- ❌ Expected: 403 Forbidden (from S3)

## Postman Collection JSON

Save this as `FeeManagementService.postman_collection.json`:

```json
{
  "info": {
    "name": "Fee Management Service",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:5001"
    },
    {
      "key": "jwtToken",
      "value": "your-jwt-token-here"
    }
  ],
  "auth": {
    "type": "bearer",
    "bearer": [
      {
        "key": "token",
        "value": "{{jwtToken}}",
        "type": "string"
      }
    ]
  },
  "item": [
    {
      "name": "Generate Presigned URL",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"feeId\": \"fee-123\",\n  \"fileName\": \"test-image.jpg\",\n  \"contentType\": \"image/jpeg\",\n  \"fileSize\": 102400\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/fees/presigned-url",
          "host": ["{{baseUrl}}"],
          "path": ["api", "fees", "presigned-url"]
        }
      }
    },
    {
      "name": "Create Fee",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"title\": \"Class Fee - Semester 1\",\n  \"description\": \"Fee for first semester\",\n  \"amount\": 1500.00,\n  \"feeType\": \"ClassFee\",\n  \"imageUrl\": \"{{imageUrl}}\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/fees",
          "host": ["{{baseUrl}}"],
          "path": ["api", "fees"]
        }
      }
    }
  ]
}
```

## Import Collection

1. Open Postman
2. Click **Import**
3. Select the `FeeManagementService.postman_collection.json` file
4. Update collection variables with your values

## Environment Setup

Create a Postman Environment for different environments:

### Development Environment

| Variable | Value |
|----------|-------|
| `baseUrl` | `https://localhost:5001` |
| `jwtToken` | `dev-token-here` |

### Production Environment

| Variable | Value |
|----------|-------|
| `baseUrl` | `https://api.yourdomain.com` |
| `jwtToken` | `prod-token-here` |

## Automated Testing

### Run Collection

1. Select the collection
2. Click **Run**
3. Select all requests
4. Click **Run Fee Management Service**

### Test Results

Postman will show:
- ✅ Passed tests (green)
- ❌ Failed tests (red)
- Test execution time
- Request/response details

## Tips

1. **Save Responses:** Use Postman's "Save Response" to save successful responses as examples
2. **Pre-request Scripts:** Add scripts to generate dynamic data (e.g., timestamps, GUIDs)
3. **Environment Variables:** Use different environments for dev/staging/prod
4. **Collection Runner:** Run entire collection for regression testing
5. **Mock Servers:** Create mock servers for frontend development

## Troubleshooting

### 401 Unauthorized
- Check JWT token is valid and not expired
- Verify token includes required claims (SchoolId, UserId, Role)
- Ensure Authorization header format: `Bearer <token>`

### 400 Bad Request
- Check request body matches API schema
- Verify all required fields are present
- Check field validation rules (max length, ranges, etc.)

### 403 Forbidden
- Verify user has "SchoolAdmin" role in JWT token
- Check presigned URL hasn't expired (10 minutes)

### Connection Errors
- Verify API is running
- Check baseUrl is correct
- Verify SSL certificate (for HTTPS)


