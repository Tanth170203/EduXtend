using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ActivityRegistration
    {
        public int Id { get; set; }

        public int ActivityId { get; set; }
        public Activity Activity { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(50)]
        public string Status { get; set; } = "Registered"; // Registered, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
