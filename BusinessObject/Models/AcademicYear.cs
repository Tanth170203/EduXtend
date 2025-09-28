using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class AcademicYear
    {
        public int Id { get; set; }
        [Required, MaxLength(20)] public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Semester> Semesters { get; set; } = new List<Semester>();
    }
}
