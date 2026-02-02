# Postman Collection Import Instructions

If you're having trouble importing the Postman collection, try these steps:

## Method 1: Direct File Import

1. Open Postman
2. Click **Import** button (top left)
3. Select **File** tab
4. Click **Upload Files**
5. Select `FeeManagementService.postman_collection.json`
6. Click **Import**

## Method 2: Copy-Paste JSON

1. Open `FeeManagementService.postman_collection.json` in a text editor
2. Copy all the JSON content
3. Open Postman
4. Click **Import** button
5. Select **Raw text** tab
6. Paste the JSON content
7. Click **Import**

## Method 3: Manual Creation

If import still fails, you can manually create the collection:

### Step 1: Create Collection

1. Click **New** → **Collection**
2. Name: `Fee Management Service`

### Step 2: Add Collection Variables

1. Select the collection
2. Go to **Variables** tab
3. Add these variables:

| Variable | Initial Value | Current Value |
|----------|---------------|---------------|
| `baseUrl` | `https://localhost:5001` | `https://localhost:5001` |
| `jwtToken` | `your-jwt-token-here` | `your-jwt-token-here` |
| `schoolId` | `550e8400-e29b-41d4-a716-446655440000` | `550e8400-e29b-41d4-a716-446655440000` |
| `userId` | `user-123` | `user-123` |
| `feeId` | `fee-123` | `fee-123` |

### Step 3: Configure Collection Authorization

1. Go to **Authorization** tab
2. Type: **Bearer Token**
3. Token: `{{jwtToken}}`

### Step 4: Create Requests

#### Request 1: Generate Presigned URL

- **Method:** POST
- **URL:** `{{baseUrl}}/api/fees/presigned-url`
- **Headers:**
  - `Content-Type: application/json`
- **Body (raw JSON):**
```json
{
  "feeId": "{{feeId}}",
  "fileName": "test-image.jpg",
  "contentType": "image/jpeg",
  "fileSize": 102400
}
```
- **Tests Tab:**
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.collectionVariables.set("presignedUrl", jsonData.presignedUrl);
    pm.collectionVariables.set("imageUrl", jsonData.imageUrl);
}
```

#### Request 2: Upload Image to S3

- **Method:** PUT
- **URL:** `{{presignedUrl}}`
- **Headers:**
  - `Content-Type: image/jpeg`
- **Body:** Select **binary** and choose an image file

#### Request 3: Create Fee

- **Method:** POST
- **URL:** `{{baseUrl}}/api/fees`
- **Headers:**
  - `Content-Type: application/json`
- **Body (raw JSON):**
```json
{
  "title": "Class Fee - Semester 1",
  "description": "Fee for first semester classes",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "{{imageUrl}}"
}
```

## Common Import Issues

### Issue: "Invalid JSON format"
**Solution:** 
- Ensure the file is saved with `.json` extension
- Check for any syntax errors
- Try Method 2 (copy-paste)

### Issue: "Collection schema not supported"
**Solution:**
- Ensure you're using Postman v8.0 or later
- Update Postman to the latest version

### Issue: "Missing required fields"
**Solution:**
- Use Method 3 (manual creation) as a workaround
- The collection will work the same way

## Verify Import

After importing, verify:
1. Collection appears in left sidebar
2. Collection variables are set correctly
3. Authorization is configured (Bearer Token)
4. All requests are present

## Next Steps

1. Update `jwtToken` variable with your actual JWT token
2. Update `baseUrl` if your API runs on a different port
3. Update `schoolId`, `userId`, `feeId` with your test data
4. Start testing!

