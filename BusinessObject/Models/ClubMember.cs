using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models
{
    public class ClubMember
    {
        public int Id { get; set; }

        public int ClubId { get; set; }
        public Club Club { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(50)]
        public string RoleInClub { get; set; } = "Member"; // Member, Manager, etc.

        public int? DepartmentId { get; set; }
        public ClubDepartment? Department { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}

