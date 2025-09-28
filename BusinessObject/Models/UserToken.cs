using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class UserToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        [Required] public string RefreshToken { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool Revoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        [MaxLength(200)] public string? DeviceInfo { get; set; }
    }
}
