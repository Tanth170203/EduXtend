# Hệ thống thông báo cho Club News

## Tổng quan
Đã thêm hệ thống thông báo tự động cho quy trình duyệt bài viết Club News.

## Luồng hoạt động

### 1. Khi Club Manager đăng bài viết mới
- **Trigger**: Club Manager tạo bài viết mới qua API `POST /api/club-news`
- **Hành động**: Hệ thống tự động gửi thông báo cho tất cả Admin
- **Nội dung thông báo**:
  - Title: "New article pending approval"
  - Message: "Club {Club Name} has posted a new article: "{Title}" and is awaiting approval."

### 2. Khi Admin duyệt/từ chối bài viết
- **Trigger**: Admin phê duyệt hoặc từ chối bài viết qua API `POST /api/club-news/{id}/approve`
- **Hành động**: Hệ thống tự động gửi thông báo cho Club Manager (người tạo bài)
- **Nội dung thông báo**:
  - Nếu được duyệt:
    - Title: "Article approved"
    - Message: "Your article "{Title}" has been approved by Admin and is now published."
  - Nếu bị từ chối:
    - Title: "Article rejected"
    - Message: "Your article "{Title}" has been rejected by Admin."

## Timezone Handling
- Hệ thống sử dụng **Vietnam Time (UTC+7)** cho tất cả timestamps
- `DateTimeHelper.Now` được dùng thay vì `DateTime.UtcNow` để đảm bảo thời gian chính xác
- Frontend hiển thị "time ago" sẽ chính xác với giờ Việt Nam

## Các file đã thêm/sửa đổi

### Files mới
1. **Services/Notifications/INotificationService.cs**
   - Interface cho NotificationService
   - Định nghĩa các method tạo thông báo

2. **Services/Notifications/NotificationService.cs**
   - Implementation của NotificationService
   - Xử lý logic tạo và gửi thông báo
   - Method `NotifyAdminsAboutNewClubNewsAsync()`: Gửi thông báo cho Admin
   - Method `NotifyClubManagerAboutNewsApprovalAsync()`: Gửi thông báo cho Club Manager

### Files đã sửa đổi
1. **Services/ClubNews/ClubNewsService.cs**
   - Thêm dependency injection cho INotificationService
   - Gọi NotificationService khi tạo bài viết mới (method `CreateAsync`)
   - Gọi NotificationService khi duyệt bài viết (method `ApproveAsync`)

2. **WebAPI/Program.cs**
   - Đăng ký NotificationService vào DI container

## Cách sử dụng

### Kiểm tra thông báo
Người dùng có thể xem thông báo của mình thông qua:
- API endpoint hiện có cho notifications (nếu đã có)
- SignalR Hub `/notificationHub` để nhận thông báo real-time

### Lưu ý
- Thông báo được lưu vào database (bảng Notifications)
- Thông báo có trạng thái đã đọc/chưa đọc
- Admin sẽ nhận được thông báo cho mọi bài viết mới từ bất kỳ CLB nào
- Club Manager chỉ nhận thông báo về bài viết của chính họ

## Testing

### Test case 1: Tạo bài viết mới
1. Login với tài khoản Club Manager
2. Tạo bài viết mới qua API `POST /api/club-news`
3. Kiểm tra: Tất cả Admin phải nhận được thông báo

### Test case 2: Duyệt bài viết
1. Login với tài khoản Admin
2. Duyệt bài viết qua API `POST /api/club-news/{id}/approve` với `approve: true`
3. Kiểm tra: Club Manager (người tạo bài) phải nhận được thông báo duyệt

### Test case 3: Từ chối bài viết
1. Login với tài khoản Admin
2. Từ chối bài viết qua API `POST /api/club-news/{id}/approve` với `approve: false`
3. Kiểm tra: Club Manager (người tạo bài) phải nhận được thông báo từ chối
