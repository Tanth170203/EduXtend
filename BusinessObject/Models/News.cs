using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class News
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = null!;
        [Required] public string Content { get; set; } = null!;
        public DateTime PublishedAt { get; set; } = DateTime.Now;
        public int? AuthorUserId { get; set; }
        public User? AuthorUser { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
