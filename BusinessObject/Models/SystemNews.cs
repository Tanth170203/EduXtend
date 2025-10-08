using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models
{
    public class SystemNews
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Content { get; set; }

        public string? ImageUrl { get; set; }
        public string? FacebookUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime PublishedAt { get; set; } = DateTime.Now;

        public int CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;
    }
}

