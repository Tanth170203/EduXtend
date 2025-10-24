using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Club;

public class UpdateClubInfoDto
{
	[MaxLength(150)]
	public string? Name { get; set; }
	[MaxLength(150)]
	public string? SubName { get; set; }
	public string? Description { get; set; }
	public string? LogoUrl { get; set; }
	public string? BannerUrl { get; set; }
	public int? CategoryId { get; set; }
	public bool? IsActive { get; set; }
}


