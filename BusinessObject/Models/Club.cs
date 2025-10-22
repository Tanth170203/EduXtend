using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Club
{
    public int Id { get; set; }
    
    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;
    
    [Required, MaxLength(150)]
    public string SubName { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public DateTime FoundedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsRecruitmentOpen { get; set; } = false;

    public int CategoryId { get; set; }
    public ClubCategory Category { get; set; } = null!;

    // Navigation properties
    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
    public ICollection<ClubDepartment> Departments { get; set; } = new List<ClubDepartment>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<ClubNews> NewsPosts { get; set; } = new List<ClubNews>();
    public ICollection<ClubAward> Awards { get; set; } = new List<ClubAward>();
    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
    public ICollection<JoinRequest> JoinRequests { get; set; } = new List<JoinRequest>();
}
