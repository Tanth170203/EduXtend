using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.JoinRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Clubs
{
    public interface IClubService
    {
        Task<List<ClubListItemDto>> GetAllClubsAsync(int? userId = null);
        Task<List<ClubListItemDto>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive, int? userId = null);
        Task<ClubDetailDto?> GetClubByIdAsync(int id);
        Task<List<string>> GetAllCategoryNamesAsync();
        Task<ClubDetailDto?> GetManagedClubByUserIdAsync(int userId);
        Task<List<ClubListItemDto>> GetAllManagedClubsByUserIdAsync(int userId);
        Task<bool> ToggleRecruitmentAsync(int clubId, bool isOpen);
        Task<RecruitmentStatusDto?> GetRecruitmentStatusAsync(int clubId);
        Task<bool> IsUserMemberOfClubAsync(int userId, int clubId);
        Task<List<ClubListItemDto>> GetClubsByUserIdAsync(int userId);
        Task<List<ClubMemberDto>> GetClubMembersAsync(int clubId);
        Task<List<DepartmentDto>> GetClubDepartmentsAsync(int clubId);
        Task<List<ClubAwardDto>> GetClubAwardsAsync(int clubId);
        Task<ClubDetailDto?> UpdateClubInfoAsync(int clubId, UpdateClubInfoDto dto);
    }
}
