using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        [Required, MaxLength(200)] public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;

        public int? ActivityId { get; set; }
        public Activity? Activity { get; set; }
    }
}
