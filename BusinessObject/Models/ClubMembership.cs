using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ClubMembership
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public Club Club { get; set; } = null!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public DateTime JoinDate { get; set; } = DateTime.Now;
        public ClubRole RoleInClub { get; set; } = ClubRole.Member;
        public MembershipStatus Status { get; set; } = MembershipStatus.Pending;
        public bool IsApproved { get; set; }
    }
}
