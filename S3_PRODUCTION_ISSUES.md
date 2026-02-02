# AWS S3 Production Issues - Interview Discussion

## Issues Faced During Implementation & Production Considerations

### 1. **Signature Mismatch Errors (SignatureDoesNotMatch)**

**Issue Encountered:**
- Error: `SignatureDoesNotMatch - The request signature we calculated does not match the signature you provided`
- Root Cause: Presigned URL was generated with `x-amz-server-side-encryption` header, but client upload request didn't include it

**What Happened:**
```csharp
// In S3Service.cs - We set ServerSideEncryptionMethod
var request = new GetPreSignedUrlRequest
{
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
};
```

When generating presigned URL with encryption, the signed headers include `x-amz-server-side-encryption`. If the client doesn't send this exact header with the exact value (`AES256`), S3 rejects the request.

**Solution:**
- Added `x-amz-server-side-encryption: AES256` header to Postman request
- Documented that any header in `X-Amz-SignedHeaders` must be included in the upload request

**Production Impact:**
- High severity - Complete upload failure
- Affects all image uploads if headers don't match
- Hard to debug without proper error logging

**Best Practices:**
- Always document which headers are required for presigned URL uploads
- Validate headers match between presigned URL generation and client upload
- Use consistent header policies across all presigned URLs

---

### 2. **Multiple Authentication Mechanisms Conflict**

**Issue Encountered:**
- Error: `InvalidArgument - Only one auth mechanism allowed; only the X-Amz-Algorithm query parameter, Signature query string parameter or the Authorization header should be specified`
- Root Cause: Postman was sending both presigned URL (with query string auth) AND Authorization header (Bearer token)

**What Happened:**
- Collection-level Bearer token auth was inherited by "Upload Image to S3" request
- Presigned URLs contain authentication in query string parameters
- S3 rejected requests with both mechanisms

**Solution:**
- Set `"auth": { "type": "noauth" }` for S3 upload request in Postman collection
- Documented that presigned URL requests should NOT include Authorization headers

**Production Impact:**
- Medium severity - Upload failures
- Common mistake when using API clients with default auth
- Can be confusing for developers

**Best Practices:**
- Clear separation between API requests (need auth) and S3 requests (presigned URL only)
- Document authentication requirements clearly
- Use different HTTP clients/configurations for API vs S3

---

### 3. **Presigned URL Expiration**

**Issue Encountered:**
- Presigned URLs expire after 10 minutes (600 seconds)
- If client takes too long to upload, URL becomes invalid

**What Happened:**
```csharp
Expires = DateTime.UtcNow.AddMinutes(expirationMinutes) // Default: 10 minutes
```

**Production Considerations:**
- **Too Short:** Users may not have time to upload large files
- **Too Long:** Security risk if URL is leaked
- **Network Issues:** Slow connections may cause timeouts

**Solutions:**
- **Dynamic Expiration:** Calculate based on file size
  ```csharp
  // Larger files = longer expiration
  var expirationMinutes = fileSize > 1MB ? 30 : 10;
  ```
- **Retry Mechanism:** Generate new presigned URL if upload fails
- **Progress Tracking:** Show upload progress to users
- **Client-Side Validation:** Check expiration before upload

**Best Practices:**
- Use 10-15 minutes for small files (< 1MB)
- Use 30-60 minutes for large files (> 1MB)
- Implement client-side expiration checks
- Provide clear error messages when URLs expire

---

### 4. **Content-Type Mismatch**

**Issue Encountered:**
- Presigned URL generated with `ContentType = "image/jpeg"`
- Client must send exact same Content-Type header
- Mismatch causes signature validation failure

**What Happened:**
```csharp
// Server generates presigned URL with:
ContentType = contentType // "image/jpeg"

// Client must send:
Content-Type: image/jpeg // Exact match required
```

**Production Considerations:**
- Different browsers/clients may normalize headers differently
- Case sensitivity issues
- MIME type variations (image/jpeg vs image/jpg)

**Solutions:**
- **Normalize Content-Type:** Convert to lowercase before generating presigned URL
- **Validate on Client:** Ensure client sends exact Content-Type
- **Document Requirements:** Clearly specify required headers

**Best Practices:**
- Always normalize Content-Type to lowercase
- Validate Content-Type matches between generation and upload
- Use standard MIME types

---

### 5. **File Size Validation**

**Issue Encountered:**
- Need to validate file size before generating presigned URL
- Client may send incorrect file size
- S3 has limits (5TB per object, but practical limits are lower)

**What Happened:**
```csharp
if (fileSize <= 0 || fileSize > MaxFileSize) // 5MB limit
    throw new ArgumentException($"File size must be between 1 byte and {MaxFileSize} bytes");
```

**Production Considerations:**
- **Client-Side Validation:** Can be bypassed
- **Server-Side Validation:** Must validate before generating presigned URL
- **Actual Upload Size:** May differ from declared size
- **Cost Implications:** Large files = higher storage/transfer costs

**Solutions:**
- **Pre-validate:** Check file size before presigned URL generation
- **Post-validate:** Verify actual upload size matches declared size
- **Progressive Upload:** Use multipart upload for large files (> 100MB)
- **Rate Limiting:** Prevent abuse

**Best Practices:**
- Validate file size on both client and server
- Use multipart upload for files > 100MB
- Implement file size limits per user/tenant
- Monitor storage costs

---

### 6. **File Extension vs Content-Type Mismatch**

**Issue Encountered:**
- File extension (`.jpg`) must match Content-Type (`image/jpeg`)
- Malicious users may try to upload executable files with image extensions

**What Happened:**
```csharp
// Validate extension
var extension = Path.GetExtension(fileName).ToLowerInvariant();
if (!AllowedExtensions.Contains(extension))
    throw new ArgumentException($"File extension must be one of: {string.Join(", ", AllowedExtensions)}");

// Validate content type
if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
    throw new ArgumentException($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}");
```

**Production Considerations:**
- **Security Risk:** Users may upload malicious files
- **Content-Type Spoofing:** Easy to fake Content-Type header
- **File Content Validation:** Should verify actual file content, not just headers

**Solutions:**
- **File Content Validation:** Use libraries to verify actual file type (e.g., check magic bytes)
- **Virus Scanning:** Scan uploaded files before making them accessible
- **Strict Validation:** Match extension, Content-Type, and actual file content
- **Quarantine:** Store suspicious files in separate bucket for review

**Best Practices:**
- Validate file content, not just headers
- Use file type detection libraries
- Implement virus scanning
- Restrict file types strictly

---

### 7. **CORS Configuration Issues**

**Issue Encountered:**
- Browser-based uploads require CORS configuration on S3 bucket
- Missing or incorrect CORS policy causes upload failures

**Production Considerations:**
- **CORS Policy:** Must allow PUT requests from your domain
- **Headers:** Must allow required headers (Content-Type, x-amz-server-side-encryption)
- **Credentials:** May need to allow credentials for authenticated uploads

**S3 CORS Configuration Example:**
```json
[
  {
    "AllowedHeaders": [
      "Content-Type",
      "x-amz-server-side-encryption"
    ],
    "AllowedMethods": [
      "PUT",
      "POST"
    ],
    "AllowedOrigins": [
      "https://yourdomain.com"
    ],
    "ExposeHeaders": [
      "ETag"
    ],
    "MaxAgeSeconds": 3000
  }
]
```

**Best Practices:**
- Configure CORS on S3 bucket
- Use specific origins, not wildcards in production
- Test CORS with actual browser requests
- Document CORS requirements

---

### 8. **Error Handling & Retry Logic**

**Issue Encountered:**
- Network failures during upload
- S3 temporary errors (503 Service Unavailable)
- No automatic retry mechanism

**Production Considerations:**
- **Transient Errors:** S3 may return 503 during high load
- **Network Issues:** Client-side network problems
- **Timeout:** Long uploads may timeout

**Solutions:**
- **Exponential Backoff:** Implement retry with exponential backoff
- **Idempotency:** Ensure retries don't create duplicate uploads
- **Progress Tracking:** Show upload progress to users
- **Resume Capability:** For large files, implement resume on failure

**Best Practices:**
- Implement retry logic with exponential backoff
- Use unique S3 keys to prevent overwrites
- Track upload progress
- Handle partial uploads gracefully

---

### 9. **Cost Optimization**

**Issue Encountered:**
- Large files = higher storage costs
- Multiple upload attempts = wasted bandwidth
- No lifecycle policies for old files

**Production Considerations:**
- **Storage Costs:** S3 charges per GB stored
- **Request Costs:** PUT requests have costs
- **Transfer Costs:** Data transfer out of S3
- **Lifecycle Management:** Old files accumulate costs

**Solutions:**
- **Lifecycle Policies:** Automatically delete or move to cheaper storage (Glacier) after X days
- **Compression:** Compress images before upload
- **CDN:** Use CloudFront to reduce transfer costs
- **Monitoring:** Track storage and transfer costs

**Best Practices:**
- Implement S3 lifecycle policies
- Compress images before upload
- Use CloudFront for image delivery
- Monitor and alert on cost anomalies

---

### 10. **Security Concerns**

**Issue Encountered:**
- Presigned URLs can be leaked
- No access control after upload
- Public URLs expose bucket structure

**Production Considerations:**
- **URL Leakage:** Presigned URLs in logs, browser history, etc.
- **Access Control:** Once uploaded, files may be publicly accessible
- **Bucket Policies:** Need to restrict access appropriately

**Solutions:**
- **Short Expiration:** Use shorter expiration times
- **Private Buckets:** Make bucket private, use CloudFront signed URLs for access
- **Access Control:** Implement bucket policies to restrict access
- **Audit Logging:** Enable S3 access logging

**Best Practices:**
- Use shortest practical expiration times
- Make S3 buckets private
- Use CloudFront signed URLs for public access
- Enable S3 access logging and CloudTrail
- Implement bucket policies

---

### 11. **Performance Issues**

**Issue Encountered:**
- Large files take time to upload
- No progress indication
- Blocking UI during upload

**Production Considerations:**
- **User Experience:** Long uploads without feedback frustrate users
- **Timeout:** Browser/server timeouts for long uploads
- **Concurrent Uploads:** Multiple files uploaded simultaneously

**Solutions:**
- **Multipart Upload:** Use S3 multipart upload for large files
- **Progress Tracking:** Show upload progress percentage
- **Async Processing:** Process uploads asynchronously
- **Chunked Upload:** Break large files into chunks

**Best Practices:**
- Use multipart upload for files > 100MB
- Implement progress tracking
- Use async/background processing
- Optimize image sizes before upload

---

### 12. **Monitoring & Debugging**

**Issue Encountered:**
- Hard to debug signature mismatches
- No visibility into S3 request failures
- Limited error information from S3

**Production Considerations:**
- **Error Messages:** S3 errors can be cryptic
- **Logging:** Need to log presigned URL generation and usage
- **Metrics:** Track success/failure rates

**Solutions:**
- **Structured Logging:** Log all S3 operations with correlation IDs
- **Error Tracking:** Use error tracking service (Sentry, etc.)
- **Metrics:** Track upload success rates, file sizes, etc.
- **S3 Access Logging:** Enable S3 server access logging

**Best Practices:**
- Log all S3 operations with correlation IDs
- Track metrics (success rate, file sizes, upload times)
- Enable S3 access logging
- Use error tracking services

---

## Summary of Key Lessons Learned

1. **Header Matching is Critical:** Any header in presigned URL's signed headers must be included in upload request
2. **No Authorization Headers:** Presigned URLs handle auth - don't add Authorization headers
3. **Expiration Management:** Balance security (short) vs usability (long)
4. **Content-Type Validation:** Must match exactly between generation and upload
5. **File Validation:** Validate file content, not just headers
6. **Error Handling:** Implement retry logic and proper error messages
7. **Security:** Use short expirations, private buckets, and signed URLs for access
8. **Monitoring:** Log everything, track metrics, enable S3 access logging

---

## Interview Talking Points

When discussing S3 production issues in interviews, focus on:

1. **Real Issues You Solved:** Signature mismatches, header conflicts, expiration
2. **Production Considerations:** Security, cost, performance, monitoring
3. **Best Practices:** What you learned and implemented
4. **Trade-offs:** Security vs usability, cost vs performance
5. **Scalability:** How you'd handle millions of uploads

---

*Document created based on actual implementation experience - February 2, 2026*

