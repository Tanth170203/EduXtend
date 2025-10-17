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
    }
}
