using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Enum;


namespace BusinessObject.Models
{
    public class Activity
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxParticipants { get; set; }

        public ActivityType ActivityType { get; set; }

        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = null!;

        public int? ClubId { get; set; }
        public Club? Club { get; set; }

        public ActivityStatus Status { get; set; } = ActivityStatus.Draft;
        public int? MaxScoreImpact { get; set; }
        [MaxLength(50)] public string? CriteriaReference { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<ActivityApproval> Approvals { get; set; } = new List<ActivityApproval>();
        public ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
        public ICollection<ActivityAttendance> Attendances { get; set; } = new List<ActivityAttendance>();
    }
}
