using System;
using System.Collections.Generic;

namespace BusinessObject.Models
{
    public class MovementRecord
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = null!;

        public double TotalScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<MovementRecordDetail> Details { get; set; } = new List<MovementRecordDetail>();
    }
}

