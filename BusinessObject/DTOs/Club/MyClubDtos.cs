namespace BusinessObject.DTOs.Club;

public class MyClubItemDto
{
	public int ClubId { get; set; }
	public string Name { get; set; } = null!;
	public string? SubName { get; set; }
	public string? LogoUrl { get; set; }
	public string CategoryName { get; set; } = null!;
	public bool IsActive { get; set; }
	public string RoleInClub { get; set; } = "Member";
	public bool IsManager { get; set; }
}

public class ClubMemberItemDto
{
	public int StudentId { get; set; }
	public string FullName { get; set; } = null!;
	public string RoleInClub { get; set; } = "Member";
	public bool IsActive { get; set; }
	public DateTime JoinedAt { get; set; }
}

public class ClubMemberManageItemDto
{
	public int Id { get; set; }
	public int StudentId { get; set; }
	public string FullName { get; set; } = null!;
	public string RoleInClub { get; set; } = "Member";
	public bool IsActive { get; set; }
	public DateTime JoinedAt { get; set; }
}


