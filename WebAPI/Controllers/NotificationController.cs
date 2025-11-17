using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Notifications;
using System.Security.Claims;
using Utils;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationRepository notificationRepo,
            ILogger<NotificationController> logger)
        {
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        // GET api/notification/my-notifications
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var notifications = await _notificationRepo.GetByUserIdAsync(userId);
                
                // Map to DTO format for frontend
                var notificationDtos = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = MapScopeToType(n.Scope),
                    scope = n.Scope,
                    targetClubId = n.TargetClubId,
                    targetRole = n.TargetRole,
                    createdAt = n.CreatedAt,
                    isRead = n.IsRead
                }).ToList();

                return Ok(notificationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { message = "Failed to get notifications" });
            }
        }

        // PUT api/notification/{id}/mark-read
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var notification = await _notificationRepo.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }

                // Verify user owns this notification
                if (notification.TargetUserId != userId)
                {
                    return Forbid();
                }

                await _notificationRepo.MarkAsReadAsync(id);
                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "Failed to mark notification as read" });
            }
        }

        // PUT api/notification/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                await _notificationRepo.MarkAllAsReadAsync(userId);
                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "Failed to mark all notifications as read" });
            }
        }

        // DELETE api/notification/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var notification = await _notificationRepo.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }

                // Verify user owns this notification
                if (notification.TargetUserId != userId)
                {
                    return Forbid();
                }

                await _notificationRepo.DeleteAsync(id);
                return Ok(new { message = "Notification deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new { message = "Failed to delete notification" });
            }
        }

        // GET api/notification/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var count = await _notificationRepo.GetUnreadCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { message = "Failed to get unread count" });
            }
        }

        // POST api/notification/test - Test endpoint to send a notification
        [HttpPost("test")]
        public async Task<IActionResult> TestNotification()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var notification = new BusinessObject.Models.Notification
                {
                    Title = "Test Notification",
                    Message = "This is a test notification to verify SignalR is working",
                    Scope = "User",
                    TargetUserId = userId,
                    CreatedById = userId,
                    IsRead = false,
                    CreatedAt = DateTimeHelper.Now
                };

                await _notificationRepo.CreateAsync(notification);
                
                return Ok(new { message = "Test notification sent", notificationId = notification.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private string MapScopeToType(string scope)
        {
            // Map notification scope to toast type for consistent UI
            return scope switch
            {
                "User" => "info",
                "Club" => "info",
                "System" => "warning",
                _ => "info"
            };
        }
    }
}
