﻿using System.ComponentModel.DataAnnotations;
using BusinessObject.Enum;

namespace BusinessObject.Models;

public class Activity
{
    public int Id { get; set; }

    // Nếu là hoạt động của CLB, sẽ có ClubId.
    // Nếu là hoạt động toàn trường (do Admin tạo), ClubId = null.
    public int? ClubId { get; set; }
    public Club? Club { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? BannerUrl { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Required]
    public ActivityType Type { get; set; }

    // ✅ CTSV chỉ duyệt các loại Event/Competition
    public bool RequiresApproval { get; set; }

    // Do ai tạo (Admin hoặc Manager)
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // Phân biệt hoạt động CLB hay toàn trường
    public bool IsPublic { get; set; } = false;

    // "PendingApproval", "Approved", "Rejected", "Completed"
    [MaxLength(50)]
    public string Status { get; set; } = "PendingApproval";

    // Nếu được duyệt thì ai duyệt
    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Số lượng người tham gia tối đa
    public int? MaxParticipants { get; set; }

    // Cho phép hệ thống tự cộng điểm phong trào khi điểm danh
    public double MovementPoint { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Liên kết dữ liệu
    public ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
    public ICollection<ActivityAttendance> Attendances { get; set; } = new List<ActivityAttendance>();
    public ICollection<ActivityFeedback> Feedbacks { get; set; } = new List<ActivityFeedback>();
    public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
}
