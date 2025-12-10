using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Club
{
    public class UpdateClubInfoDto
    {
        [Required]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(150)]
        public string SubName { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? BannerUrl { get; set; }
    }
}
