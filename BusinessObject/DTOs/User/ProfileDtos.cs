using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.User;

public class ProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Url]
    public string? AvatarUrl { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^\d*$", ErrorMessage = "Phone number must contain digits only.")]
    public string? PhoneNumber { get; set; }
}


