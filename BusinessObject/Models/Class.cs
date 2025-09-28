using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Class
    {
        public int Id { get; set; }
        [Required, MaxLength(50)] public string ClassCode { get; set; } = null!;
        [Required, MaxLength(200)] public string Name { get; set; } = null!;

        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; } = default!;

        public int? MonitorStudentId { get; set; }
        public Student? MonitorStudent { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
