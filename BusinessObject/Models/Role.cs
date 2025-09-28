using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Role
    {
        public int Id { get; set; }
        [Required, MaxLength(50)] public string RoleName { get; set; } = null!;
        [MaxLength(200)] public string? Description { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
