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
        public Activity Activity { get; set; } = default!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;

        public bool IsAttended { get; set; }
        public bool IsValid { get; set; }
        public int ScoreImpact { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
    }
}
