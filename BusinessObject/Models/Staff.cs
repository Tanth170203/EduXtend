using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Staff
    {
        public int Id { get; set; }
        //public string StaffCode { get; set; } = null!;
        [Required, MaxLength(100)] public string? FullName { get; set; }
        [EmailAddress] public string? Email { get; set; }
        [MaxLength(15)] public string? Phone { get; set; }

        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public StaffType Type { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? FacultyId { get; set; }
        public Faculty? Faculty { get; set; }

        public ICollection<Class> AdvisorOfClasses { get; set; } = new List<Class>();
    }
}
