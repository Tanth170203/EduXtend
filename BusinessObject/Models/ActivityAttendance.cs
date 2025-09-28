using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ActivityAttendance
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity Activity { get; set; } = default!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;
        public bool Present { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        [MaxLength(200)] public string? CheckInMethod { get; set; }
    }
}
