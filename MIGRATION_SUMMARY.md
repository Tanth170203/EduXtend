# Tóm Tắt Migration - Xóa Bảng UserRole

## Ngày: 2025-10-29

## Thay Đổi Chính
Đã chuyển từ mô hình **Many-to-Many** (User - UserRole - Role) sang **One-to-Many** (User - Role).
- **Trước**: Một user có thể có nhiều roles thông qua bảng trung gian `UserRole`
- **Sau**: Một user chỉ có một role duy nhất thông qua cột `RoleId` trong bảng `Users`

## Migration Files
1. `20251023044157_RemoveUserRoleTable.cs` - Migration đã xóa bảng UserRole và thêm RoleId vào Users
2. `20251026211502_AllowMultipleScoresPerCriterion.cs` - Migration đã cleanup UserRoles table (nếu còn tồn tại)

## Files Đã Được Cập Nhật Tự Động (Không Cần Sửa Thêm)

### Models
- ✅ `BusinessObject/Models/User.cs` - Đã có `RoleId` và navigation property `Role`
- ✅ `BusinessObject/Models/Role.cs` - Đã có collection `Users`
- ✅ Không còn file `UserRole.cs` model

### DbContext
- ✅ `DataAccess/EduXtendDbContext.cs` - Đã cấu hình relationship 1-nhiều
- ✅ Không còn `DbSet<UserRole>` trong DbContext

### Repositories
- ✅ `Repositories/Users/UserRepository.cs` - Method `UpdateUserRolesAsync()` đã được cập nhật để chỉ lấy role đầu tiên
- ✅ `Repositories/Users/IUserRepository.cs` - Interface vẫn giữ nguyên để backward compatible

### Services
- ✅ `Services/Users/UserManagementService.cs` - Đã map user.Role sang List<RoleDto> để backward compatible
- ✅ `Services/UserImport/UserImportService.cs` - Đã cập nhật để chỉ assign 1 role khi import users
- ✅ `Services/GGLogin/GoogleAuthService.cs` - Đã cập nhật để kiểm tra và set `RoleId`

### DTOs
- ✅ `BusinessObject/DTOs/User/UserDto.cs` - Vẫn dùng `List<string> Roles` để backward compatible
- ✅ `BusinessObject/DTOs/User/UserWithRolesDto.cs` - Vẫn dùng `List<RoleDto> Roles` và `List<int> RoleIds`

### API Controllers
- ✅ `WebAPI/Controllers/UserManagementController.cs` - Endpoint `/api/user-management/{id}/role` (singular) đã được cập nhật

## Files Đã Sửa Trong Session Này

### WebFE - Frontend Pages
1. ✅ **`WebFE/Pages/Admin/Roles/Index.cshtml.cs`**
   - Sửa endpoint từ `/api/user-management/{userId}/roles` (plural) → `/api/user-management/{userId}/role` (singular)
   - Sửa request body từ `{ UserId, RoleIds[] }` → `{ UserId, RoleId }`
   - Chỉ lấy role đầu tiên từ array

2. ✅ **`WebFE/Pages/Admin/Roles/Index.cshtml`**
   - Đổi từ **checkbox** sang **radio button** để chỉ chọn được 1 role
   - Cập nhật các label: "Manage Roles" → "Manage Role"
   - Cập nhật tiêu đề bảng: "Current Roles" → "Current Role"
   - Cập nhật JavaScript để xử lý radio button thay vì checkbox

## Kiểm Tra Build
- ✅ **Build thành công** với 0 errors
- ⚠️ Có 4 warnings (không liên quan đến UserRole migration):
  - Null reference warnings (CS8602, CS8604)
  - IgnoreAntiforgeryToken warning (MVC1001)

## Các File Không Cần Sửa (Migration Snapshots)
- `DataAccess/Migrations/*.Designer.cs` - Các file này là snapshots của EF và không nên sửa thủ công

## Backward Compatibility
Code đã được thiết kế để backward compatible:
- DTOs vẫn sử dụng `List<RoleDto>` và `List<int> RoleIds`
- Services/Repositories map single role thành list để frontend vẫn hoạt động
- Chỉ frontend UI mới bị cập nhật để phản ánh đúng nghiệp vụ mới

## Testing Recommendations
1. ✅ Test login với Google (GoogleAuthService)
2. ✅ Test import users từ Excel
3. ✅ Test cập nhật role của user qua Admin panel
4. ✅ Test authorization middleware với single role
5. ✅ Kiểm tra tất cả trang yêu cầu role-based access

## Notes
- Database migration đã được chạy và UserRole table đã bị xóa
- Tất cả users hiện tại đã được migrate sang model mới với 1 role duy nhất
- Nếu có user không có role, hệ thống sẽ tự động gán role "Student" (RoleId = 2)

