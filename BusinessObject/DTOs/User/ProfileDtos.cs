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

	[MaxLength(10)]
	[RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 numbers.")]
    public string? PhoneNumber { get; set; }
}


