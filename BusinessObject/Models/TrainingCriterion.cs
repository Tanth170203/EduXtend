using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class TrainingCriterion
    {
        public int Id { get; set; }
        [Required, MaxLength(20)] public string Code { get; set; } = null!;
        [Required, MaxLength(300)] public string Name { get; set; } = null!;
        public int MaxPoints { get; set; }
        public int Order { get; set; }

        public int? ParentId { get; set; }
        public TrainingCriterion? Parent { get; set; }
        public ICollection<TrainingCriterion> Children { get; set; } = new List<TrainingCriterion>();
    }
}

