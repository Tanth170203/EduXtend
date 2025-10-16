# Cập nhật Import User với thông tin Student

## Tổng quan thay đổi

### 1. Thêm cột "Cohort" (Khóa) vào bảng Student
- **Cột mới**: `Cohort` (MaxLength: 10)
- **Ví dụ giá trị**: K17, K18, K19, K20, K21, K22...
- **Mục đích**: Phân loại sinh viên theo khóa học

### 2. Loại bỏ các cột trùng lặp
- **Đã xóa**: `FullName`, `Email`, `Phone` khỏi bảng Student
- **Thay thế**: Sử dụng computed properties để truy cập từ bảng User
- **Lợi ích**: Giảm trùng lặp dữ liệu, dễ bảo trì

### 3. Cập nhật User Import Service
- **Tự động tạo Student record** khi user có role "Student"
- **Validation đầy đủ** cho thông tin sinh viên
- **Hỗ trợ import hàng loạt** với thông tin sinh viên

## Format Excel mới

### Cột bắt buộc:
- **A**: Email (required)
- **B**: Full Name (required)
- **C**: Phone Number (optional)
- **D**: Roles (optional, comma-separated)
- **E**: Is Active (optional, true/false/1/0/yes/no)

### Cột cho Student (bắt buộc nếu role có "Student"):
- **F**: Student Code (required for Student role)
- **G**: Cohort (required for Student role, e.g., "K17", "K18", "K20")
- **H**: Date of Birth (optional, format: YYYY-MM-DD)
- **I**: Gender (optional, Male/Female/Other, default: Other)
- **J**: Enrollment Date (optional, format: YYYY-MM-DD, default: current date)
- **K**: Major Code (required for Student role, e.g., "SE", "IA", "AI")
- **L**: Student Status (optional, Active/Inactive/Graduated, default: Active)

## Ví dụ dữ liệu

```csv
Email,FullName,PhoneNumber,Roles,IsActive,StudentCode,Cohort,DateOfBirth,Gender,EnrollmentDate,MajorCode,StudentStatus
student1@fpt.edu.vn,John Doe,0123456789,Student,true,SE12345,K20,2000-05-15,Male,2020-09-01,SE,Active
admin@fpt.edu.vn,Admin User,0901234567,Admin,true,,,,,,,
manager@fpt.edu.vn,Club Manager,0912345678,"ClubManager,Student",true,SE99999,K18,1998-03-10,Male,2018-09-01,SE,Active
```

## Các thay đổi kỹ thuật

### 1. Model Student
```csharp
public class Student
{
    // ... existing properties ...
    
    [Required, MaxLength(10)]
    public string Cohort { get; set; } = null!; // K17, K18, K20, etc.
    
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;
    
    [EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;
    
    [MaxLength(15)]
    public string? Phone { get; set; }
}
```

### 2. Repository mới
- `IStudentRepository` / `StudentRepository`
- `IMajorRepository` / `MajorRepository`

### 3. UserImportService cập nhật
- Tự động tạo Student record khi role có "Student"
- Validation đầy đủ cho thông tin sinh viên
- Xử lý lỗi chi tiết

## Migration cần thiết

```bash
cd EduXtend/DataAccess
dotnet ef migrations add AddCohortColumnAndRemoveDuplicateColumns --startup-project ../WebAPI
dotnet ef database update --startup-project ../WebAPI
```

## Lợi ích

1. **Giảm trùng lặp dữ liệu**: Loại bỏ các cột trùng lặp giữa User và Student
2. **Phân loại sinh viên**: Cột Cohort giúp phân loại theo khóa học
3. **Import tự động**: Tự động tạo Student record khi import user có role Student
4. **Validation mạnh mẽ**: Kiểm tra đầy đủ thông tin sinh viên
5. **Tính mở rộng**: Dễ dàng thêm các loại user khác (Staff, Teacher) trong tương lai

## Lưu ý

- **Backup database** trước khi chạy migration
- **Kiểm tra dữ liệu hiện tại** có thể bị ảnh hưởng
- **Cập nhật frontend** để hiển thị thông tin Cohort
- **Training người dùng** về format Excel mới
