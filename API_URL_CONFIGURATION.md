# 🔗 **API URL CONFIGURATION - WebAPI & WebFE**

## **Created: 17/10/2025**

---

## 📌 **PORT MAPPING**

| Service | Port | Protocol | BaseAddress |
|---------|------|----------|------------|
| **WebAPI** | 5001 | HTTPS | `https://localhost:5001` |
| **WebFE** | 3001 | HTTPS | `https://localhost:3001` |

---

## ⚠️ **IMPORTANT: API CALLS FROM FRONTEND**

### ❌ **WRONG - Relative URL**
```javascript
// This will call https://localhost:3001/api/students (WRONG!)
const response = await fetch('/api/students');
```

**Why?** Because WebFE runs on port 3001, so relative URLs use that port as base.

---

### ✅ **CORRECT - Absolute URL**
```javascript
// This will call https://localhost:5001/api/students (CORRECT!)
const response = await fetch('https://localhost:5001/api/students', {
    method: 'GET',
    credentials: 'include',  // Include cookies (JWT tokens)
    headers: {
        'Accept': 'application/json'
    }
});
```

---

## 📝 **API ENDPOINTS - COMPLETE LIST**

### **New Endpoints (Scoring Modal)**
```
GET    https://localhost:5001/api/students
GET    https://localhost:5001/api/students/{id}
GET    https://localhost:5001/api/students/search?query={query}
```

### **Existing Endpoints**
```
GET    https://localhost:5001/api/movement-records
GET    https://localhost:5001/api/movement-records/{id}
GET    https://localhost:5001/api/movement-records/student/{studentId}
GET    https://localhost:5001/api/movement-records/semester/{semesterId}
POST   https://localhost:5001/api/movement-records/add-score
PUT    https://localhost:5001/api/movement-records/{id}
DELETE https://localhost:5001/api/movement-records/{id}

GET    https://localhost:5001/api/evidences
GET    https://localhost:5001/api/evidences/{id}
GET    https://localhost:5001/api/evidences/pending
POST   https://localhost:5001/api/evidences

GET    https://localhost:5001/api/semesters
GET    https://localhost:5001/api/semesters/{id}
GET    https://localhost:5001/api/semesters/active

GET    https://localhost:5001/api/movement-criteria
```

---

## 🔧 **CONFIGURATION FILES**

### **1. WebFE/Program.cs** - HttpClient Setup
```csharp
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001"); // ✅ Backend API base
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
    );
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler
    {
        UseCookies = true,
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    return handler;
});
```

### **2. WebFE/appsettings.json** - API Settings
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

### **3. WebFE/wwwroot/js/auth.js** - JavaScript Auth Utility
```javascript
const API_BASE_URL = "https://localhost:5001";

async function getUser() {
    const res = await fetch(`${API_BASE_URL}/api/auth/me`, {
        method: 'GET',
        credentials: 'include'
    });
    return res.ok ? await res.json() : null;
}
```

### **4. WebFE/Pages/Admin/MovementReports/Index.cshtml** - Scoring Modal
```javascript
// ✅ CORRECT: Use absolute URL
async function loadStudentsForScoring() {
    const response = await fetch('https://localhost:5001/api/students', {
        method: 'GET',
        credentials: 'include',
        headers: { 'Accept': 'application/json' }
    });
    // ...
}
```

---

## 🛠️ **HOW TO USE IN DIFFERENT CONTEXTS**

### **Context 1: Server-Side (C# / Razor Pages)**
```csharp
// In Index.cshtml.cs (PageModel)
private HttpClient CreateHttpClient() {
    var handler = new HttpClientHandler {
        UseCookies = true,
        ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
    };
    
    var client = new HttpClient(handler) {
        BaseAddress = new Uri("https://localhost:5001") // ✅
    };
    
    return client;
}

// Usage
var response = await client.GetAsync("/api/students");
```

### **Context 2: Client-Side (JavaScript / Fetch)**
```javascript
// Option A: Use full absolute URL
await fetch('https://localhost:5001/api/students', {
    credentials: 'include'
});

// Option B: Use API_BASE_URL from auth.js
await fetch(`${API_BASE_URL}/api/students`, {
    credentials: 'include'
});

// Option C: Use apiRequest helper (from admin-dashboard.js)
await apiRequest('https://localhost:5001/api/students', {
    method: 'GET'
});
```

### **Context 3: PageModel Handler (OnPostAsync)**
```csharp
public async Task<IActionResult> OnPostAddManualScoreAsync(
    int studentId, int categoryId, double score, string comments, string awardedDate)
{
    try {
        var client = CreateHttpClient(); // Uses https://localhost:5001
        var dto = new AddScoreDto { ... };
        
        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync("/api/movement-records/add-score", content);
        
        if (response.IsSuccessStatusCode) {
            SuccessMessage = "✅ Đã cộng điểm thành công!";
            return RedirectToPage();
        }
        
        ErrorMessage = "❌ Lỗi: " + response.Content.ReadAsStringAsync().Result;
        return Page();
    }
    catch (Exception ex) {
        ErrorMessage = $"❌ Lỗi: {ex.Message}";
        return Page();
    }
}
```

---

## ✅ **AUTHENTICATION & COOKIES**

### **Important**: Always include `credentials: 'include'`
```javascript
fetch('https://localhost:5001/api/students', {
    credentials: 'include' // ✅ Include httpOnly cookies (JWT tokens)
});
```

This ensures:
- JWT access token is sent in cookies
- JWT refresh token is handled automatically
- CORS credentials are allowed

---

## 🚀 **TESTING API ENDPOINTS**

### **Using cURL** (with Admin JWT token)
```bash
# Get all students
curl -X GET "https://localhost:5001/api/students" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Accept: application/json" \
  -k  # -k = ignore SSL certificate warnings (dev only)

# Search students
curl -X GET "https://localhost:5001/api/students/search?query=Nguyễn" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -k
```

### **Using Postman**
1. **URL**: `https://localhost:5001/api/students`
2. **Method**: GET
3. **Headers**:
   - `Authorization: Bearer YOUR_TOKEN`
   - `Accept: application/json`
4. **SSL Verification**: Disable (dev mode)
5. **Send**

### **Using Browser DevTools**
```javascript
// Open browser console (F12) and paste:
fetch('https://localhost:5001/api/students', {
    method: 'GET',
    credentials: 'include'
})
.then(r => r.json())
.then(data => console.log(data));
```

---

## 🔐 **SSL CERTIFICATE HANDLING**

### **Development (localhost)**
- Self-signed certificates are normal
- Must disable SSL verification in code

**C# HttpClient Setup**:
```csharp
ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
```

**JavaScript Fetch**:
```bash
# In cURL, use -k flag to ignore SSL errors
curl -k https://localhost:5001/api/students
```

### **Production**
- Use valid SSL certificates
- Remove certificate validation bypass
- Enable HTTPS only

---

## 📊 **COMPLETE REQUEST/RESPONSE EXAMPLE**

### **Request: Get All Students**
```http
GET /api/students HTTP/1.1
Host: localhost:5001
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Accept: application/json
Connection: keep-alive
```

### **Response: 200 OK**
```json
[
  {
    "id": 1,
    "studentCode": "SE160001",
    "fullName": "Nguyễn Văn A",
    "email": "nguyenvana@fpt.edu.vn",
    "cohort": "K16"
  },
  {
    "id": 2,
    "studentCode": "SE160002",
    "fullName": "Trần Thị B",
    "email": "tranthib@fpt.edu.vn",
    "cohort": "K16"
  }
]
```

### **Error Response: 401 Unauthorized**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "The HTTP request was unauthorized."
}
```

### **Error Response: 403 Forbidden**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.2",
  "title": "Forbidden",
  "status": 403,
  "detail": "User does not have the required role."
}
```

---

## 🎯 **QUICK CHECKLIST**

When calling an API from WebFE:

```
☐ Use ABSOLUTE URL: https://localhost:5001/api/...
☐ Include credentials: include
☐ Add Accept header: application/json
☐ Add Authorization header if using bearer token
☐ Handle 401 (unauthorized) → refresh token
☐ Handle 403 (forbidden) → check user role
☐ Handle 404 (not found) → check API path
☐ Use try-catch for network errors
☐ Log errors for debugging
```

---

## 📝 **FILES MODIFIED FOR API CALLS**

- ✅ `EduXtend/WebFE/wwwroot/js/auth.js` - Sets `API_BASE_URL`
- ✅ `EduXtend/WebFE/Program.cs` - HttpClient configuration
- ✅ `EduXtend/WebFE/appsettings.json` - API settings
- ✅ `EduXtend/WebFE/Pages/Admin/MovementReports/Index.cshtml` - **FIXED** scoring modal API calls
- ✅ `EduXtend/WebAPI/Controllers/StudentController.cs` - New `/api/students` endpoints

---

## 🚨 **COMMON ERRORS & SOLUTIONS**

### **Error 1: 404 Not Found**
```
Message: POST https://localhost:3001/api/students 404
```
**Cause**: Using relative URL from port 3001  
**Solution**: Change to `https://localhost:5001/api/students`

---

### **Error 2: 401 Unauthorized**
```
Message: {"status":401,"title":"Unauthorized"}
```
**Cause**: JWT token missing or expired  
**Solution**: Include `credentials: 'include'` or add token to header

---

### **Error 3: CORS Error**
```
Message: Access to XMLHttpRequest blocked by CORS policy
```
**Cause**: CORS not configured, or credentials not included  
**Solution**:
- WebAPI must have CORS enabled
- Use `credentials: 'include'`
- Check browser console for exact error

---

### **Error 4: SSL Certificate Error**
```
Message: certificate_verify_failed
```
**Cause**: Self-signed certificate in development  
**Solution**:
- C#: Add `ServerCertificateCustomValidationCallback = (m,c,ch,e) => true`
- Browser: Click "Advanced" and proceed
- cURL: Use `-k` flag

---

## 📚 **REFERENCES**

- WebFE Configuration: `EduXtend/WebFE/Program.cs`
- API Documentation: `EduXtend/API_ENDPOINTS_SCORING.md`
- Test Guide: `EduXtend/TEST_GUIDE_COMPLETE.md`
- Implementation Summary: `EduXtend/IMPLEMENTATION_SUMMARY.md`

---

**Status**: CONFIGURATION COMPLETE ✅  
**Version**: 1.0  
**Last Updated**: 17/10/2025  
**Next Step**: Run tests following TEST_GUIDE_COMPLETE.md
