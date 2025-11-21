# Missing Features - Club Collaboration Selection

## Vấn đề (Problems)

### 1. UI Fields không hiển thị (FIXED ✓)

Khi chọn Activity Type = "Club Collaboration" trong form tạo activity (Create.cshtml), các fields sau không hiển thị:
- Collaborating Club selection
- Collaboration Points input

**Nguyên nhân:** Function `updateCollaborationFieldsVisibility()` được định nghĩa **sau** khi `updateActivityTypeUI()` được gọi lần đầu, nên khi chạy lần đầu function chưa tồn tại.

**Đã fix:** Di chuyển phần "Collaboration Fields Management" lên trước phần "Mandatory Button & Club Activity Type Logic" trong Create.cshtml.

### 2. API Endpoint URL sai (FIXED ✓)

Modal `_ClubSelectionModal.cshtml` đang gọi sai URL:
```javascript
// SAI
const url = `${apiBaseUrl}/api/admin/activities/available-clubs?excludeClubId=${excludeClubId || 0}`;

// ĐÚNG
const url = `${apiBaseUrl}/api/activity/available-clubs?excludeClubId=${excludeClubId || 0}`;
```

**Nguyên nhân:** Modal gọi endpoint của Admin (`/api/admin/activities/available-clubs`) nhưng endpoint thực tế là `/api/activity/available-clubs` (dùng chung cho cả Admin và Club Manager).

**Đã fix:** Đổi URL trong `_ClubSelectionModal.cshtml` từ `/api/admin/activities/available-clubs` thành `/api/activity/available-clubs`.

## Giải pháp (Solution)

Cần implement đầy đủ các layer sau:

### 1. Repository Layer

**File:** `Repositories/Activities/IActivityRepository.cs`

Thêm method:
```csharp
Task<List<Club>> GetAvailableCollaboratingClubsAsync(int excludeClubId);
```

**File:** `Repositories/Activities/ActivityRepository.cs`

Implement:
```csharp
public async Task<List<Club>> GetAvailableCollaboratingClubsAsync(int excludeClubId)
{
    return await _context.Clubs
        .Where(c => c.Id != excludeClubId && c.IsActive) // Exclude specified club and only active clubs
        .OrderBy(c => c.Name)
        .Select(c => new Club
        {
            Id = c.Id,
            Name = c.Name,
            LogoUrl = c.LogoUrl,
            // Include member count if needed
        })
        .ToListAsync();
}
```

### 2. Service Layer

**File:** `Services/Activities/IActivityService.cs`

Thêm method:
```csharp
Task<List<ClubListDto>> GetAvailableCollaboratingClubsAsync(int excludeClubId);
```

**File:** `Services/Activities/ActivityService.cs`

Implement:
```csharp
public async Task<List<ClubListDto>> GetAvailableCollaboratingClubsAsync(int excludeClubId)
{
    var clubs = await _activityRepository.GetAvailableCollaboratingClubsAsync(excludeClubId);
    
    return clubs.Select(c => new ClubListDto
    {
        Id = c.Id,
        Name = c.Name,
        LogoUrl = c.LogoUrl,
        MemberCount = c.ClubMembers?.Count ?? 0
    }).ToList();
}
```

### 3. DTO

**File:** `BusinessObject/DTOs/Club/ClubListDto.cs` (nếu chưa có)

```csharp
namespace BusinessObject.DTOs.Club
{
    public class ClubListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int MemberCount { get; set; }
    }
}
```

### 4. API Controller

**File:** `WebAPI/Controllers/ActivitiesController.cs`

Thêm endpoint:
```csharp
[HttpGet("available-clubs")]
[Authorize] // Both Admin and Club Manager can access
public async Task<IActionResult> GetAvailableClubs([FromQuery] int excludeClubId = 0)
{
    try
    {
        var clubs = await _activityService.GetAvailableCollaboratingClubsAsync(excludeClubId);
        return Ok(clubs);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting available clubs");
        return StatusCode(500, "An error occurred while retrieving clubs");
    }
}
```

### 5. Update Modal (nếu cần)

**File:** `WebFE/Pages/Shared/_ClubSelectionModal.cshtml`

Đổi URL từ:
```javascript
const url = `${apiBaseUrl}/api/admin/activities/available-clubs?excludeClubId=${excludeClubId || 0}`;
```

Thành:
```javascript
const url = `${apiBaseUrl}/api/activity/available-clubs?excludeClubId=${excludeClubId || 0}`;
```

## Testing

Sau khi implement, test như sau:

1. Login as Club Manager
2. Navigate to Create Activity page
3. Select Activity Type = "Club Collaboration"
4. Click "Select Club" button
5. Modal should open and show list of clubs (excluding your own club)
6. Search functionality should work
7. Select a club and confirm
8. Selected club name should appear in the form

## Priority

**HIGH** - Feature không thể sử dụng được nếu thiếu endpoint này.


## Testing Checklist

### Test UI Fix (Create.cshtml)

1. ✓ Navigate to Club Manager > Create Activity
2. ✓ Select Activity Type = "Club Collaboration"
3. ✓ Verify "Collaborating Club" section appears
4. ✓ Verify "Collaboration Points" input appears
5. ✓ Verify "Movement Points" input appears
6. ⚠️ Click "Select Club" button → Modal opens but no clubs listed (API missing)

### After API Implementation

7. Click "Select Club" button
8. Modal should open with list of clubs (excluding your club)
9. Search for a club name
10. Select a club
11. Verify club name appears in the form
12. Enter collaboration points (1-3)
13. Enter movement points (1-10)
14. Submit form
15. Verify activity created with collaboration data

## Status

- [x] UI Fields Display Issue - FIXED
- [x] API Endpoint URL - FIXED
- [ ] Full Integration Test - READY FOR TESTING

## Summary

Tất cả các vấn đề đã được fix:
1. ✓ Di chuyển code JavaScript để function được định nghĩa trước khi gọi
2. ✓ Sửa URL API endpoint từ `/api/admin/activities/available-clubs` thành `/api/activity/available-clubs`

Backend đã có đầy đủ implementation:
- ✓ Repository: `GetAvailableCollaboratingClubsAsync` 
- ✓ Service: `GetAvailableCollaboratingClubsAsync`
- ✓ Controller: `GET /api/activity/available-clubs`

Feature sẵn sàng để test!
