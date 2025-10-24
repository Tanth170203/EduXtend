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
        Task<List<MyClubItemDto>> GetMyClubsAsync(int userId);
        Task<List<ClubMemberItemDto>> GetClubMembersAsync(int clubId);
        Task<bool> LeaveClubAsync(int userId, int clubId);
        Task<List<ClubMemberManageItemDto>> GetMembersForManageAsync(int clubId);
        Task<bool> UpdateMemberRoleAsync(int clubId, int studentId, string role);
        Task<bool> RemoveMemberAsync(int clubId, int studentId);
        Task<bool> UpdateClubInfoAsync(int clubId, UpdateClubInfoDto dto);
        Task<List<CategoryItemDto>> GetAllCategoriesAsyncLite();
    }
}
