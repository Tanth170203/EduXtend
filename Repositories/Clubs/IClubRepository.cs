using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Clubs
{
    public interface IClubRepository
    {
        Task<List<Club>> GetAllAsync();
        Task<List<Club>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive);
        Task<Club?> GetByIdAsync(int id);
        Task<Club?> GetByIdWithDetailsAsync(int id);
        Task<int> GetMemberCountAsync(int clubId);
        Task<int> GetActivityCountAsync(int clubId);
        Task<List<ClubCategory>> GetAllCategoriesAsync();
        Task<Club?> GetManagedClubByUserIdAsync(int userId);
        Task<bool> ToggleRecruitmentAsync(int clubId, bool isOpen);
        Task<int> GetPendingRequestCountAsync(int clubId);
        Task<List<(Club Club, string RoleInClub)>> GetClubsByUserIdAsync(int userId);
        Task<List<(int StudentId, string FullName, string RoleInClub, bool IsActive, DateTime JoinedAt)>> GetClubMembersAsync(int clubId);
        Task<bool> LeaveClubAsync(int userId, int clubId);
        Task<List<(int Id, int StudentId, string FullName, string RoleInClub, bool IsActive, DateTime JoinedAt)>> GetMembersForManageAsync(int clubId);
        Task<bool> UpdateMemberRoleAsync(int clubId, int studentId, string role);
        Task<bool> RemoveMemberAsync(int clubId, int studentId);
        Task<bool> UpdateClubInfoAsync(int clubId, string? name, string? subName, string? description, string? logoUrl, string? bannerUrl, int? categoryId, bool? isActive);
        Task<List<(int Id, string Name)>> GetAllCategoriesAsyncLite();
    }
}
