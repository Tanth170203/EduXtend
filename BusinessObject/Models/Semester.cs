using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Semester
    {
        public int Id { get; set; }
        
        [Required, MaxLength(20)] 
        public string Name { get; set; } = null!; // e.g., Fall2025, Spring2026
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = false;
    }
}
