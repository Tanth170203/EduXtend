namespace BusinessObject.DTOs.ImportFile
{
    public class UserImportRow
    {
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; } // Phone number for user
        public string? Roles { get; set; } // Comma-separated roles: "Student", "Admin", etc.
        public bool IsActive { get; set; } = true;
        
        // Student-specific fields (only used when role includes "Student")
        public string? StudentCode { get; set; }
        public string? Cohort { get; set; } // K17, K18, K20, etc.
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // Male, Female, Other
        public DateTime? EnrollmentDate { get; set; }
        public string? MajorCode { get; set; } // SE, IA, AI, etc.
        public string? StudentStatus { get; set; } // Active, Inactive, Graduated, etc.
    }
}

