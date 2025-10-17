using BusinessObject.DTOs.Club;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Clubs
{
    public interface IClubService
    {
        Task<List<ClubListItemDto>> GetAllClubsAsync();
        Task<ClubDetailDto?> GetClubByIdAsync(int id);
    }
}
