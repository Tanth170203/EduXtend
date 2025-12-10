using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.News;

public class NewsListItemDto
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? ImageUrl { get; set; }
	public bool IsPublished { get; set; }
	public DateTime? PublishedAt { get; set; }
}

public class NewsDetailDto
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Content { get; set; }
	public string? ImageUrl { get; set; }
	public string? FacebookUrl { get; set; }
	public bool IsPublished { get; set; }
	public DateTime? PublishedAt { get; set; }
	public int CreatedById { get; set; }
	public string? CreatedByName { get; set; }
}

public class CreateNewsRequest
{
	[Required, MaxLength(200)]
	public string Title { get; set; } = string.Empty;
	public string? Content { get; set; }
	[Url]
	public string? ImageUrl { get; set; }
	[Url]
	public string? FacebookUrl { get; set; }
	public bool Publish { get; set; } = false;
}

public class UpdateNewsRequest
{
	[Required, MaxLength(200)]
	public string Title { get; set; } = string.Empty;
	public string? Content { get; set; }
	[Url]
	public string? ImageUrl { get; set; }
	[Url]
	public string? FacebookUrl { get; set; }
	public bool? Publish { get; set; }
}

public class PublishNewsRequest
{
	public bool Publish { get; set; }
}


