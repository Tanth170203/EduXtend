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
        Task<bool> IsUserMemberOfClubAsync(int userId, int clubId);
        Task<List<Club>> GetClubsByUserIdAsync(int userId);
        Task<List<ClubMember>> GetClubMembersAsync(int clubId);
        Task<List<ClubDepartment>> GetClubDepartmentsAsync(int clubId);
        Task<List<ClubAward>> GetClubAwardsAsync(int clubId);
        Task<string?> GetUserRoleInClubAsync(int userId, int clubId);
        Task UpdateAsync(Club club);
    }
}
