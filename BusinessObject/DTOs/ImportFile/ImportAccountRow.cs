using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.ImportFile
{
    public class ImportAccountRow
    {
        public string Email { get; set; } = null!;
        public string FullName { get; set; }
        public string RoleNames { get; set; } = null!; // separated by comma, e.g., "Student,Admin"
        public string StudentCode { get; set; } = null!;
        public string StaffCode { get; set; } = null!;
        public string DepartmentCode { get; set; } = null!; // e.g., "D001"
        public string FacultyCode { get; set; } = null!; // e.g., "F001"

    }
}
