using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.Club
{
    public class ClubDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? SubName { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime FoundedDate { get; set; }
        public string CategoryName { get; set; } = null!;
        
        // Statistics
        public int MemberCount { get; set; }
        public int ActivityCount { get; set; }
        public int DepartmentCount { get; set; }
        public int AwardCount { get; set; }
        
        // Member role distribution
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
        
        // Activities (optional - can be loaded separately)
        public List<Activity.ActivityListItemDto>? Activities { get; set; }
    }
}
