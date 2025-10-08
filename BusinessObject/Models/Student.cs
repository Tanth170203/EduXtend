using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string StudentCode { get; set; } = null!;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(15)]
        public string? Phone { get; set; }

        public DateTime EnrollmentDate { get; set; }
        public StudentStatus Status { get; set; }

        // User relationship (1-1)
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Major relationship
        public int MajorId { get; set; }
        public Major Major { get; set; } = null!;

        // Navigation properties
        public ICollection<MovementRecord> MovementRecords { get; set; } = new List<MovementRecord>();
        public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
    }
}
