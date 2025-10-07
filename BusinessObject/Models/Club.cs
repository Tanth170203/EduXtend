using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
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

        public DateTime FoundedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public ClubCategory Category { get; set; } = null!;

        // Navigation properties
        public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
        public ICollection<ClubDepartment> Departments { get; set; } = new List<ClubDepartment>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<Plan> Plans { get; set; } = new List<Plan>();
        public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
        public ICollection<ClubNews> NewsPosts { get; set; } = new List<ClubNews>();
    }
}

