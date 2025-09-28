using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ClassMeeting
    {
        public int Id { get; set; }
        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = default!;

        public int OrganizedById { get; set; } // Class Monitor (Student.UserId)
        public User OrganizedBy { get; set; } = default!;

        public DateTime MeetingDate { get; set; }
        [MaxLength(300)] public string? MeetingUrl { get; set; }
        [MaxLength(500)] public string? Note { get; set; }
    }
}
