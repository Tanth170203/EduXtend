using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.News;

public class ClubNewsListItemDto
{
	public int Id { get; set; }
	public int ClubId { get; set; }
	public string ClubName { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? ImageUrl { get; set; }
	public bool IsApproved { get; set; }
	public DateTime PublishedAt { get; set; }
	public int CreatedById { get; set; }
	public string? CreatedByName { get; set; }
}

public class ClubNewsDetailDto
{
	public int Id { get; set; }
	public int ClubId { get; set; }
	public string ClubName { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? Content { get; set; }
	public string? ImageUrl { get; set; }
	public string? FacebookUrl { get; set; }
	public bool IsApproved { get; set; }
	public DateTime PublishedAt { get; set; }
	public int CreatedById { get; set; }
	public string? CreatedByName { get; set; }
}

public class CreateClubNewsRequest
{
	[Required, MaxLength(200)]
	public string Title { get; set; } = string.Empty;
	
	[MaxLength(1000)]
	public string? Content { get; set; }
	
	[Url]
	public string? ImageUrl { get; set; }
	
	[Url]
	public string? FacebookUrl { get; set; }
}

public class UpdateClubNewsRequest
{
	[Required, MaxLength(200)]
	public string Title { get; set; } = string.Empty;
	
	[MaxLength(1000)]
	public string? Content { get; set; }
	
	[Url]
	public string? ImageUrl { get; set; }
	
	[Url]
	public string? FacebookUrl { get; set; }
}

public class ApproveClubNewsRequest
{
	public bool Approve { get; set; }
}
