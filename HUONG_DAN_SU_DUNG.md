# HƯỚNG DẪN SỬ DỤNG - IMPORT USER & LOGIN MỚI

## 📋 TÓM TẮT THAY ĐỔI

Đã hoàn thành 2 yêu cầu:

### ✅ 1. Bỏ kiểm tra email @fpt.edu.vn khi đăng nhập
- **Trước đây:** Hệ thống chỉ chấp nhận email có đuôi `@fpt.edu.vn`
- **Bây giờ:** Hệ thống chấp nhận mọi email, miễn là đã được đăng ký trong database

### ✅ 2. Thay đổi cơ chế đăng nhập
- **Trước đây:** Tự động tạo tài khoản mới khi đăng nhập lần đầu
- **Bây giờ:** Hiển thị thông báo lỗi nếu email chưa được đăng ký
  ```
  "Email của bạn chưa được đăng ký trong hệ thống. 
   Vui lòng liên hệ quản trị viên để được hỗ trợ."
  ```

### ✅ 3. Thêm tính năng Import Users hàng loạt cho Admin
- Admin có thể import danh sách user từ file Excel
- Hỗ trợ import trước mỗi năm học mới
- Có validation và báo lỗi chi tiết

---

## 🚀 CÁCH SỬ DỤNG IMPORT USER

### Bước 1: Chuẩn bị file Excel

Tạo file Excel (.xlsx hoặc .xls) với cấu trúc sau:

**Dòng 1 (Header):**
```
Email | FullName | Roles | IsActive
```

**Dòng 2 trở đi (Dữ liệu):**
```
student1@fpt.edu.vn | Nguyễn Văn A | Student | true
student2@fpt.edu.vn | Trần Thị B | Student | true
admin@fpt.edu.vn | Quản Trị Viên | Admin,Student | true
```

#### Mô tả các cột:

| Cột | Bắt buộc | Mô tả | Ví dụ |
|-----|----------|-------|-------|
| **Email** | ✅ Có | Email đăng nhập (không được trùng) | student@fpt.edu.vn |
| **FullName** | ✅ Có | Họ tên đầy đủ | Nguyễn Văn A |
| **Roles** | ❌ Không | Vai trò (ngăn cách bởi dấu phẩy) | Student,ClubMember |
| **IsActive** | ❌ Không | Trạng thái kích hoạt | true/false |

#### Lưu ý:
- Email phải duy nhất, không được trùng với email đã có
- Nếu không điền Roles, hệ thống tự gán role "Student"
- Nếu không điền IsActive, mặc định là "true"
- Roles hợp lệ: `Admin`, `Student`, `ClubManager`, `ClubMember`

### Bước 2: Tải template mẫu (Tùy chọn)

Gọi API để tải file mẫu:

```http
GET /api/admin/userimport/template
Authorization: Bearer {admin_access_token}
```

Hoặc dùng file mẫu: `sample_user_import_template.csv`

### Bước 3: Import file

```http
POST /api/admin/userimport/import
Authorization: Bearer {admin_access_token}
Content-Type: multipart/form-data

Body:
- File: file_excel_cua_ban.xlsx
```

### Bước 4: Kiểm tra kết quả

Hệ thống sẽ trả về kết quả:

**Thành công 100%:**
```json
{
  "message": "Import thành công 50 user",
  "data": {
    "totalRows": 50,
    "successCount": 50,
    "failureCount": 0,
    "errors": []
  }
}
```

**Có lỗi:**
```json
{
  "message": "Import hoàn tất với một số lỗi. Thành công: 45/50",
  "data": {
    "totalRows": 50,
    "successCount": 45,
    "failureCount": 5,
    "errors": [
      {
        "rowNumber": 7,
        "email": "duplicate@fpt.edu.vn",
        "errorMessage": "Email đã tồn tại trong hệ thống"
      },
      {
        "rowNumber": 10,
        "email": "",
        "errorMessage": "Email không được để trống"
      }
    ]
  }
}
```

---

## 📋 DANH SÁCH API

### 1. Import Users
```
POST /api/admin/userimport/import
Quyền: Admin
Body: File Excel (.xlsx hoặc .xls)
```

### 2. Tải Template Mẫu
```
GET /api/admin/userimport/template
Quyền: Admin
Trả về: File CSV mẫu
```

### 3. Lấy Danh Sách Roles
```
GET /api/admin/userimport/roles
Quyền: Admin
Trả về: ["Admin", "Student", "ClubManager", "ClubMember"]
```

---

## ⚠️ CÁC LỖI THƯỜNG GẶP

### Lỗi 1: "Email không được để trống"
**Nguyên nhân:** Ô email bị trống  
**Giải pháp:** Điền email vào ô trống

### Lỗi 2: "Họ tên không được để trống"
**Nguyên nhân:** Ô họ tên bị trống  
**Giải pháp:** Điền họ tên vào ô trống

### Lỗi 3: "Email đã tồn tại trong hệ thống"
**Nguyên nhân:** Email này đã được import hoặc đăng ký trước đó  
**Giải pháp:** Bỏ dòng này hoặc sửa email khác

### Lỗi 4: "Role 'XXX' không tồn tại trong hệ thống"
**Nguyên nhân:** Tên role không hợp lệ  
**Giải pháp:** Sử dụng đúng tên role: Admin, Student, ClubManager, ClubMember

### Lỗi 5: "Chỉ chấp nhận file Excel (.xlsx, .xls)"
**Nguyên nhân:** File upload không phải Excel  
**Giải pháp:** Chuyển file sang định dạng Excel

### Lỗi 6: "File Excel không có dữ liệu"
**Nguyên nhân:** Sheet Excel trống  
**Giải pháp:** Thêm dữ liệu vào sheet

---

## 💡 VÍ DỤ FILE EXCEL

### Ví dụ 1: Import danh sách sinh viên
```
Email                  | FullName           | Roles   | IsActive
-----------------------|--------------------|---------|----------
sv001@fpt.edu.vn      | Nguyễn Văn An      | Student | true
sv002@fpt.edu.vn      | Trần Thị Bình      | Student | true
sv003@fpt.edu.vn      | Lê Văn Cường       | Student | true
```

### Ví dụ 2: Import với nhiều roles
```
Email                  | FullName           | Roles              | IsActive
-----------------------|--------------------|--------------------|---------
admin@fpt.edu.vn      | Quản Trị Viên      | Admin,Student      | true
qlclb@fpt.edu.vn      | Quản Lý CLB        | ClubManager,Student| true
tvclb@fpt.edu.vn      | Thành Viên CLB     | ClubMember,Student | true
```

### Ví dụ 3: Import user không active
```
Email                  | FullName           | Roles   | IsActive
-----------------------|--------------------|---------|----------
nghihoc@fpt.edu.vn    | Sinh Viên Nghỉ     | Student | false
```

---

## 🔄 QUY TRÌNH IMPORT TRƯỚC NĂM HỌC MỚI

### 1. Chuẩn bị
- [ ] Lấy danh sách sinh viên từ phòng đào tạo
- [ ] Lấy danh sách giảng viên (nếu cần)
- [ ] Tải template mẫu từ hệ thống

### 2. Tạo file Excel
- [ ] Tạo file Excel theo đúng format
- [ ] Điền đầy đủ Email và FullName
- [ ] Gán Roles phù hợp (nếu cần)
- [ ] Kiểm tra không có email trùng lặp

### 3. Import vào hệ thống
- [ ] Đăng nhập với tài khoản Admin
- [ ] Upload file Excel
- [ ] Đợi hệ thống xử lý

### 4. Kiểm tra kết quả
- [ ] Xem số lượng import thành công
- [ ] Nếu có lỗi, tải xuống danh sách lỗi
- [ ] Sửa các dòng bị lỗi
- [ ] Import lại các dòng đã sửa

### 5. Thông báo sinh viên
- [ ] Gửi email thông báo hệ thống đã sẵn sàng
- [ ] Hướng dẫn sinh viên đăng nhập
- [ ] Hỗ trợ sinh viên gặp vấn đề

---

## 🧪 KIỂM TRA SAU KHI TRIỂN KHAI

### Kiểm tra Login
1. Thử đăng nhập với email không phải @fpt.edu.vn (nhưng đã import)
   - ✅ Kỳ vọng: Đăng nhập thành công

2. Thử đăng nhập với email chưa được import
   - ✅ Kỳ vọng: Hiển thị lỗi "Email chưa được đăng ký..."

3. Thử đăng nhập với email @fpt.edu.vn đã import
   - ✅ Kỳ vọng: Đăng nhập thành công

### Kiểm tra Import
1. Import file với 10 users hợp lệ
   - ✅ Kỳ vọng: 10/10 thành công

2. Import file với email trùng lặp
   - ✅ Kỳ vọng: Báo lỗi email đã tồn tại

3. Import file thiếu trường bắt buộc
   - ✅ Kỳ vọng: Báo lỗi trường bắt buộc

4. Import file với role không hợp lệ
   - ✅ Kỳ vọng: Báo lỗi role không tồn tại

---

## 📞 HỖ TRỢ

Nếu gặp vấn đề, vui lòng:
1. Kiểm tra lại định dạng file Excel
2. Đọc kỹ thông báo lỗi
3. Xem file `IMPORT_USERS_GUIDE.md` để biết thêm chi tiết
4. Liên hệ: admin@fpt.edu.vn

---

## 📝 GHI CHÚ QUAN TRỌNG

⚠️ **Lưu ý:**
- Chỉ Admin mới có quyền import users
- Email phải duy nhất trong toàn hệ thống
- Không thể cập nhật user đã tồn tại qua import (chỉ tạo mới)
- File Excel tối đa 28.6 MB
- Dữ liệu thành công sẽ được lưu ngay, không rollback nếu có lỗi

💡 **Mẹo:**
- Nên import theo từng lô nhỏ (50-100 users) để dễ kiểm soát lỗi
- Backup database trước khi import số lượng lớn
- Kiểm tra kỹ file trước khi import
- Sử dụng template mẫu để đảm bảo đúng format

---

**Ngày cập nhật:** 14/10/2025  
**Phiên bản:** 1.1.0  
**Trạng thái:** ✅ Sẵn sàng sử dụng

