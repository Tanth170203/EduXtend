using BusinessObject.Enum;

namespace BusinessObject.DTOs.Activity;

public class ActivityDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ActivityType Type { get; set; }
    public bool? RequiresApproval { get; set; }
    public bool IsPublic { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class CreateActivityDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ActivityType Type { get; set; }
    public bool IsPublic { get; set; }
    public string? ImageUrl { get; set; }
}


