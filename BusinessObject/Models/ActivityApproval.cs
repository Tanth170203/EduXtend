using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ActivityApproval
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity Activity { get; set; } = default!;
        public int StaffId { get; set; }
        public Staff Staff { get; set; } = default!;
        public ApprovalAction Action { get; set; }
        [MaxLength(1000)] public string? Note { get; set; }
        public DateTime ApprovedAt { get; set; } = DateTime.Now;
    }
}
