namespace BusinessObject.DTOs.User;

public class UserWithRolesDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
    public List<int> RoleIds { get; set; } = new();
}

public class RoleDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
}

