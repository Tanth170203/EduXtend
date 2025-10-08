using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Message { get; set; }

        public string Scope { get; set; } = "Club"; // Club / System

        public int? TargetClubId { get; set; }
        public Club? TargetClub { get; set; }

        [MaxLength(50)]
        public string? TargetRole { get; set; } // Member / Manager / All

        public int CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
