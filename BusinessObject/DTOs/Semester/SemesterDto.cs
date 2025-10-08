using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.Semester
{
    public class SemesterDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSemesterDto
    {
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // IsActive sẽ được tự động tính toán ở backend dựa trên StartDate và EndDate
        
        // Force = true sẽ bỏ qua kiểm tra overlap
        public bool Force { get; set; } = false;
    }

    public class UpdateSemesterDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // IsActive sẽ được tự động tính toán ở backend dựa trên StartDate và EndDate
        
        // Force = true sẽ bỏ qua kiểm tra overlap
        public bool Force { get; set; } = false;
    }
}

