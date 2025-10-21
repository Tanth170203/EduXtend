using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.User;

public class UpdateUserRolesDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public List<int> RoleIds { get; set; } = new();
}

