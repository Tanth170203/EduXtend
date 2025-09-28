using BusinessObject.Enum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!; // must be SSO email

        [MaxLength(150)] public string? FullName { get; set; }
        [MaxLength(64)] public string? GoogleSubject { get; set; } // SSO sub claim

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public Student? Student { get; set; }
        public Staff? Staff { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<UserToken> Tokens { get; set; } = new List<UserToken>();
    }
}
