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
        [Required, MaxLength(255)] public string Name { get; set; } = null!;
        [Required, MaxLength(50)] public string ShortName { get; set; } = null!;
        [MaxLength(255)] public string? LogoUrl { get; set; }
        [MaxLength(255)] public string? CoverImageUrl { get; set; }
        [Required] public string Description { get; set; } = null!;
        public string? Mission { get; set; }
        public string? Achievements { get; set; }
        [Required, MaxLength(120)] public string ContactEmail { get; set; } = null!;
        [MaxLength(255)] public string? ContactFacebook { get; set; }
        [MaxLength(255)] public string? ContactOther { get; set; }
        public ClubStatus Status { get; set; } = ClubStatus.Active;

        public DateTime FoundingDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<ClubMembership> Members { get; set; } = new List<ClubMembership>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
