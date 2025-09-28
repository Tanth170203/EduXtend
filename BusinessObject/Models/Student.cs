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
        [Required, MaxLength(20)] public string StudentCode { get; set; } = null!;
        [Required, MaxLength(100)] public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        [EmailAddress] public string? Email { get; set; }
        [MaxLength(15)] public string? Phone { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public StudentStatus Status { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public int ClassId { get; set; }
        public Class Class { get; set; } = default!;
        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; } = default!;

        public ICollection<ClubMembership> ClubMemberships { get; set; } = new List<ClubMembership>();
        public ICollection<ActivityRegistration> ActivityRegistrations { get; set; } = new List<ActivityRegistration>();
        public ICollection<TrainingEvaluation> TrainingEvaluations { get; set; } = new List<TrainingEvaluation>();
        public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
    }
}
