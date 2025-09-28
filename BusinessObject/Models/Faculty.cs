using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Faculty
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = null!;
        [MaxLength(20)] public string? Code { get; set; }
        public ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}
