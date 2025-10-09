namespace BusinessObject.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? GoogleSubject { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public List<UserRoleDto>? UserRoles { get; set; }
    }

    public class UserRoleDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public RoleDto? Role { get; set; }
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class UpdateRoleRequest
    {
        public int RoleId { get; set; }
    }
}

