namespace BusinessObject.DTOs.Club
{
    public class ClubMemberDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string RoleInClub { get; set; } = "Member";
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
    }
}

