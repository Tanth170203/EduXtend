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
        Task<List<ClubListItemDto>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive);
        Task<ClubDetailDto?> GetClubByIdAsync(int id);
        Task<List<string>> GetAllCategoryNamesAsync();
        Task<ClubDetailDto?> GetManagedClubByUserIdAsync(int userId);
        Task<bool> ToggleRecruitmentAsync(int clubId, bool isOpen);
        Task<RecruitmentStatusDto?> GetRecruitmentStatusAsync(int clubId);
    }
}
