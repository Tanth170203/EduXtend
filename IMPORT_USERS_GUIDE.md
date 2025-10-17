# Hướng dẫn Import Users Hàng Loạt

## Tổng Quan

Chức năng import users cho phép quản trị viên nhập danh sách người dùng hàng loạt từ file Excel trước mỗi năm học mới.

## Thay Đổi Trong Login

### 1. Bỏ kiểm tra email domain @fpt.edu.vn
- **Trước:** Hệ thống chỉ chấp nhận email có đuôi `@fpt.edu.vn`
- **Sau:** Hệ thống chấp nhận mọi email, chỉ cần đã được import vào database

### 2. Kiểm tra user tồn tại
- **Trước:** Tự động tạo user mới khi login lần đầu
- **Sau:** Hiển thị thông báo lỗi nếu email chưa được đăng ký trong hệ thống
  ```
  "Your email is not registered in the system. Please contact the administrator for support."
  ```

## Sử Dụng Chức Năng Import

### API Endpoints (Admin Only)

#### 1. Import Users từ Excel
```http
POST /api/admin/userimport/import
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data

Body:
- File: excel_file.xlsx
```

**Response Success:**
```json
{
  "message": "Import thành công 50 user",
  "data": {
    "totalRows": 50,
    "successCount": 50,
    "failureCount": 0,
    "errors": [],
    "successMessages": [
      "Row 2: student1@fpt.edu.vn - Nguyen Van A",
      "Row 3: student2@fpt.edu.vn - Tran Thi B"
    ]
  }
}
```

**Response With Errors:**
```json
{
  "message": "Import hoàn tất với một số lỗi. Thành công: 45/50",
  "data": {
    "totalRows": 50,
    "successCount": 45,
    "failureCount": 5,
    "errors": [
      {
        "rowNumber": 3,
        "email": "duplicate@fpt.edu.vn",
        "errorMessage": "Email already exists in the system"
      },
      {
        "rowNumber": 7,
        "email": "",
        "errorMessage": "Email cannot be empty"
      }
    ],
    "successMessages": [...]
  }
}
```

#### 2. Tải Template Mẫu
```http
GET /api/admin/userimport/template
Authorization: Bearer {admin_token}
```

Trả về file CSV mẫu để làm template.

#### 3. Lấy Danh Sách Roles
```http
GET /api/admin/userimport/roles
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "message": "List of available roles for import",
  "roles": ["Admin", "Student", "ClubManager", "ClubMember"]
}
```

### Định Dạng File Excel

#### Cấu Trúc File

| Column | Tên Cột | Bắt Buộc | Mô Tả | Ví Dụ |
|--------|----------|----------|-------|-------|
| A | Email | ✅ Có | Email của user | student1@fpt.edu.vn |
| B | FullName | ✅ Có | Họ và tên đầy đủ | Nguyễn Văn A |
| C | Roles | ❌ Không | Danh sách roles (phân cách bằng dấu phẩy) | Student,ClubMember |
| D | IsActive | ❌ Không | Trạng thái active (true/false/1/0/yes/no) | true |

#### Ví Dụ File Excel

```
Email                    | FullName        | Roles              | IsActive
------------------------|-----------------|--------------------|---------
student1@fpt.edu.vn     | Nguyễn Văn A    | Student            | true
student2@fpt.edu.vn     | Trần Thị B      | Student            | true
admin@fpt.edu.vn        | Admin User      | Admin,Student      | true
manager@fpt.edu.vn      | Club Manager    | ClubManager,Student| true
inactive@fpt.edu.vn     | Inactive User   | Student            | false
```

#### Lưu Ý Quan Trọng

1. **Row đầu tiên** (row 1) là header, dữ liệu bắt đầu từ row 2
2. **Email phải unique** - không được trùng với email đã có trong database
3. **Roles mặc định:** Nếu không chỉ định role, hệ thống tự động gán role "Student"
4. **IsActive mặc định:** Nếu không chỉ định, mặc định là `true`
5. **Format file:** Chấp nhận `.xlsx` hoặc `.xls`

### Validation Rules

#### Email
- ✅ Bắt buộc
- ✅ Không được trùng với email đã tồn tại
- ✅ Format hợp lệ

#### FullName
- ✅ Bắt buộc
- ✅ Tối đa 100 ký tự

#### Roles
- ❌ Không bắt buộc
- ✅ Phải là role hợp lệ trong hệ thống
- ✅ Có thể nhiều roles, phân cách bằng dấu phẩy
- ✅ Roles hợp lệ: "Admin", "Student", "ClubManager", "ClubMember"

#### IsActive
- ❌ Không bắt buộc
- ✅ Giá trị hợp lệ: true/false, 1/0, yes/no (không phân biệt hoa thường)

## Quy Trình Import Trước Năm Học Mới

### Bước 1: Chuẩn Bị Dữ Liệu
1. Lấy danh sách sinh viên/giảng viên từ phòng đào tạo
2. Tạo file Excel theo đúng format
3. Kiểm tra dữ liệu: email hợp lệ, không trùng lặp

### Bước 2: Download Template (Optional)
```bash
curl -X GET "https://your-api.com/api/admin/userimport/template" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  --output template.csv
```

### Bước 3: Import File
```bash
curl -X POST "https://your-api.com/api/admin/userimport/import" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -F "File=@userlist.xlsx"
```

### Bước 4: Kiểm Tra Kết Quả
- Xem response để biết số lượng import thành công
- Nếu có lỗi, kiểm tra danh sách errors và fix các row bị lỗi
- Import lại các row bị lỗi sau khi đã sửa

## Error Messages

| Lỗi | Nguyên Nhân | Giải Pháp |
|-----|-------------|-----------|
| "Email cannot be empty" | Empty email cell | Fill email in cell |
| "Full name cannot be empty" | Empty fullname cell | Fill full name in cell |
| "Email already exists in the system" | Email already imported | Remove this row or update existing user |
| "Role 'XXX' does not exist in the system" | Invalid role name | Use correct role name (see role list) |
| "Invalid or empty file" | File upload error | Check file again |
| "Only Excel files (.xlsx, .xls) are accepted" | File is not Excel | Convert to Excel format |
| "Excel file has no data" | Empty sheet | Add data to sheet |

## Technical Details

### Files Created/Modified

#### New Files
- `EduXtend/BusinessObject/DTOs/ImportFile/ImportUsersRequest.cs`
- `EduXtend/BusinessObject/DTOs/ImportFile/ImportUsersResponse.cs`
- `EduXtend/BusinessObject/DTOs/ImportFile/UserImportRow.cs`
- `EduXtend/Services/UserImport/IUserImportService.cs`
- `EduXtend/Services/UserImport/UserImportService.cs`
- `EduXtend/WebAPI/Admin/UserManagement/UserImportController.cs`

#### Modified Files
- `EduXtend/Services/GGLogin/GoogleAuthService.cs`
  - Removed @fpt.edu.vn email validation
  - Changed to throw error if user doesn't exist
- `EduXtend/Repositories/Users/IUserRepository.cs`
  - Added bulk operation methods
- `EduXtend/Repositories/Users/UserRepository.cs`
  - Implemented bulk operation methods
- `EduXtend/WebAPI/Program.cs`
  - Registered UserImportService

### Dependencies
- EPPlus (for Excel reading) - Make sure it's installed in Services project

## Testing

### Test Case 1: Import Successful
1. Create Excel with 10 valid users
2. POST to `/api/admin/userimport/import`
3. Verify response shows successCount = 10
4. Check database for new users

### Test Case 2: Duplicate Email
1. Import user with email `test@fpt.edu.vn`
2. Try to import same email again
3. Verify error message about duplicate email

### Test Case 3: Missing Required Field
1. Create Excel with empty email
2. Import file
3. Verify error message about required field

### Test Case 4: Login with Unregistered Email
1. Try to login with email not in database
2. Verify error: "Your email is not registered in the system..."

### Test Case 5: Login with Registered Email (No Domain Check)
1. Import user with any email domain (not @fpt.edu.vn)
2. Login with that email
3. Verify successful login

## Support

Nếu gặp vấn đề, liên hệ:
- Email: admin@fpt.edu.vn
- Hotline: xxx-xxx-xxxx

