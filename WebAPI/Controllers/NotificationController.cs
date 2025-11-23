using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.Models;
using BusinessObject.DTOs.Notification;
using Services.Notifications;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService service,
            ILogger<NotificationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // Helper method to get current user ID
        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return null;
            return userId;
        }

        /// <summary>
        /// GET api/notifications/unread
        /// Get unread notifications for current user
        /// Requirements: 7.1, 7.2
        /// </summary>
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var notifications = await _service.GetUnreadNotificationsAsync(userId.Value);
                var dtos = notifications.Select(MapToDto).ToList();
                return Ok(new { data = dtos, count = dtos.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications");
                return StatusCode(500, new { message = "An error occurred while retrieving unread notifications" });
            }
        }

        /// <summary>
        /// GET api/notifications?page=1&pageSize=10
        /// Get paginated notifications for current user
        /// Requirements: 7.3
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Validate pagination parameters
                if (page < 1)
                    page = 1;
                if (pageSize < 1 || pageSize > 100)
                    pageSize = 10;

                var notifications = await _service.GetNotificationsAsync(userId.Value, page, pageSize);
                var dtos = notifications.Select(MapToDto).ToList();
                return Ok(new { data = dtos, page, pageSize, count = dtos.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        /// <summary>
        /// PUT api/notifications/{id}/read
        /// Mark notification as read
        /// Requirements: 7.4
        /// </summary>
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var notification = await _service.MarkNotificationAsReadAsync(id);
                var dto = MapToDto(notification);
                return Ok(new { message = "Thông báo đã được đánh dấu là đã đọc", data = dto });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "An error occurred while marking notification as read" });
            }
        }

        /// <summary>
        /// PUT api/notifications/read-all
        /// Mark all notifications as read for current user
        /// Requirements: 7.4
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                await _service.MarkAllAsReadAsync(userId.Value);
                return Ok(new { message = "Tất cả thông báo đã được đánh dấu là đã đọc" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "An error occurred while marking all notifications as read" });
            }
        }

        /// <summary>
        /// GET api/notifications/unread-count
        /// Get count of unread notifications for current user
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var count = await _service.GetUnreadCountAsync(userId.Value);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { message = "An error occurred while retrieving unread count" });
            }
        }

        /// <summary>
        /// Map Notification model to NotificationDto
        /// </summary>
        private NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = DetermineNotificationType(notification.Title),
                Scope = notification.Scope,
                ReportId = null, // Will be set by service if needed
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        /// <summary>
        /// Determine notification type based on title
        /// </summary>
        private string DetermineNotificationType(string title)
        {
            if (title.Contains("được nộp") || title.Contains("submitted"))
                return "ReportSubmitted";
            if (title.Contains("được duyệt") || title.Contains("approved"))
                return "ReportApproved";
            if (title.Contains("bị từ chối") || title.Contains("rejected"))
                return "ReportRejected";
            return "info";
        }
    }
}
