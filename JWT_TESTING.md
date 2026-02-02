# JWT Token Testing Guide

## JWT Configuration

The application is configured to validate JWT tokens with the following settings:

- **Issuer:** `FeeManagementService`
- **Audience:** `FeeManagementService`
- **Secret Key:** Configured in `appsettings.json` (must be at least 32 characters)
- **Expiration:** 60 minutes (configurable via `ExpirationMinutes`)

## Required JWT Claims

For the application to work correctly, JWT tokens must include the following claims:

### Custom Claims:
- **SchoolId** (string/Guid) - Required: The school/tenant identifier
- **UserId** (string) - Required: The user identifier
- **Role** (string) - Required: User role (e.g., "SchoolAdmin")

### Standard Claims:
- **sub** (subject) - User identifier
- **exp** (expiration) - Token expiration time (Unix timestamp)
- **iat** (issued at) - Token issued time (Unix timestamp)
- **iss** (issuer) - Must match: `FeeManagementService`
- **aud** (audience) - Must match: `FeeManagementService`

## Sample JWT Token Structure

```json
{
  "sub": "user-123",
  "iss": "FeeManagementService",
  "aud": "FeeManagementService",
  "exp": 1735689600,
  "iat": 1735686000,
  "SchoolId": "550e8400-e29b-41d4-a716-446655440000",
  "UserId": "user-123",
  "Role": "SchoolAdmin"
}
```

## Testing JWT Tokens

### Option 1: Using jwt.io

1. Go to https://jwt.io
2. In the **PAYLOAD** section, paste the JSON structure above (update values as needed)
3. In the **VERIFY SIGNATURE** section:
   - Set the algorithm to `HS256`
   - Enter your secret key from `appsettings.json` in the secret field
4. Copy the generated token from the **Encoded** section
5. Use this token in the `Authorization` header: `Bearer <token>`

### Option 2: Generate Token Programmatically

You can use the following C# code to generate a test token:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var secretKey = "your-super-secret-key-that-should-be-at-least-32-characters-long-for-jwt-signing";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var claims = new[]
{
    new Claim("sub", "user-123"),
    new Claim("SchoolId", "550e8400-e29b-41d4-a716-446655440000"),
    new Claim("UserId", "user-123"),
    new Claim("Role", "SchoolAdmin"),
    new Claim(JwtRegisteredClaimNames.Iss, "FeeManagementService"),
    new Claim(JwtRegisteredClaimNames.Aud, "FeeManagementService"),
    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
    new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
};

var token = new JwtSecurityToken(
    issuer: "FeeManagementService",
    audience: "FeeManagementService",
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(60),
    signingCredentials: creds
);

var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
```

### Option 3: Using Swagger UI

1. Start the application
2. Navigate to `/swagger`
3. Click the **Authorize** button
4. Enter: `Bearer <your-jwt-token>`
5. Click **Authorize**
6. Now you can test the endpoints with authentication

## Testing Endpoints

### Test Presigned URL Endpoint

```http
POST /api/fees/presigned-url
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "feeId": "fee-123",
  "fileName": "test-image.jpg",
  "contentType": "image/jpeg",
  "fileSize": 102400
}
```

### Test Create Fee Endpoint

```http
POST /api/fees
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "title": "Test Fee",
  "description": "Test fee description",
  "amount": 100.50,
  "feeType": "ClassFee",
  "imageUrl": "https://school-platform-fees-vasanth.s3.us-east-1.amazonaws.com/schools/{schoolId}/fees/{feeId}/image.jpg"
}
```

## Common Issues

### 401 Unauthorized
- **Cause:** Token is missing, expired, or invalid
- **Solution:** 
  - Check token is included in `Authorization: Bearer <token>` header
  - Verify token hasn't expired
  - Ensure secret key matches between token generation and appsettings.json

### 401 Unauthorized - SchoolId not found
- **Cause:** Token doesn't include `SchoolId` claim
- **Solution:** Ensure token includes `SchoolId` claim with a valid GUID

### 403 Forbidden
- **Cause:** User doesn't have required role (`SchoolAdmin`)
- **Solution:** Ensure token includes `Role: "SchoolAdmin"` claim

## Security Notes

⚠️ **Important:**
- Never commit real JWT secret keys to source control
- Use strong secret keys (at least 256 bits / 32 characters)
- In production, use environment variables or secrets manager
- Rotate secret keys regularly
- Use HTTPS in production to protect tokens in transit

