using BusinessObject.DTOs.Club;
using Repositories.Clubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Clubs
{
    public class ClubService : IClubService
    {
        private readonly IClubRepository _repo;
        public ClubService(IClubRepository repo) => _repo = repo;

        public async Task<List<ClubListItemDto>> GetAllClubsAsync()
        {
            var clubs = await _repo.GetAllAsync();
            var result = new List<ClubListItemDto>();

            foreach (var c in clubs)
            {
                result.Add(new ClubListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    SubName = c.SubName,
                    LogoUrl = c.LogoUrl,
                    CategoryName = c.Category.Name,
                    IsActive = c.IsActive,
                    IsRecruitmentOpen = c.IsRecruitmentOpen,
                    FoundedDate = c.FoundedDate,
                    Description = c.Description,
                    MemberCount = await _repo.GetMemberCountAsync(c.Id),
                    ActivityCount = await _repo.GetActivityCountAsync(c.Id)
                });
            }

            return result;
        }

        public async Task<List<ClubListItemDto>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive)
        {
            var clubs = await _repo.SearchClubsAsync(searchTerm, categoryName, isActive);
            var result = new List<ClubListItemDto>();

            foreach (var c in clubs)
            {
                result.Add(new ClubListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    SubName = c.SubName,
                    LogoUrl = c.LogoUrl,
                    CategoryName = c.Category.Name,
                    IsActive = c.IsActive,
                    IsRecruitmentOpen = c.IsRecruitmentOpen,
                    FoundedDate = c.FoundedDate,
                    Description = c.Description,
                    MemberCount = await _repo.GetMemberCountAsync(c.Id),
                    ActivityCount = await _repo.GetActivityCountAsync(c.Id)
                });
            }

            return result;
        }

        public async Task<List<string>> GetAllCategoryNamesAsync()
        {
            var categories = await _repo.GetAllCategoriesAsync();
            return categories.Select(c => c.Name).ToList();
        }

        public async Task<ClubDetailDto?> GetClubByIdAsync(int id)
        {
            var c = await _repo.GetByIdWithDetailsAsync(id);
            if (c == null) return null;

            var roleDistribution = c.Members
                .Where(m => m.IsActive)
                .GroupBy(m => m.RoleInClub)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ClubDetailDto
            {
                Id = c.Id,
                Name = c.Name,
                SubName = c.SubName,
                Description = c.Description,
                LogoUrl = c.LogoUrl,
                BannerUrl = c.BannerUrl,
                IsActive = c.IsActive,
                IsRecruitmentOpen = c.IsRecruitmentOpen,
                FoundedDate = c.FoundedDate,
                CategoryName = c.Category.Name,
                MemberCount = c.Members.Count(m => m.IsActive),
                ActivityCount = c.Activities.Count,
                DepartmentCount = c.Departments.Count,
                AwardCount = c.Awards.Count,
                RoleDistribution = roleDistribution
            };
        }

        public async Task<ClubDetailDto?> GetManagedClubByUserIdAsync(int userId)
        {
            var club = await _repo.GetManagedClubByUserIdAsync(userId);
            if (club == null) return null;

            // Get detailed info
            return await GetClubByIdAsync(club.Id);
        }

        public async Task<bool> ToggleRecruitmentAsync(int clubId, bool isOpen)
        {
            return await _repo.ToggleRecruitmentAsync(clubId, isOpen);
        }

        public async Task<RecruitmentStatusDto?> GetRecruitmentStatusAsync(int clubId)
        {
            var club = await _repo.GetByIdAsync(clubId);
            if (club == null) return null;

            var pendingCount = await _repo.GetPendingRequestCountAsync(clubId);

            return new RecruitmentStatusDto
            {
                ClubId = club.Id,
                ClubName = club.Name,
                IsRecruitmentOpen = club.IsRecruitmentOpen,
                PendingRequestCount = pendingCount
            };
        }

        public async Task<List<MyClubItemDto>> GetMyClubsAsync(int userId)
        {
            var list = await _repo.GetClubsByUserIdAsync(userId);
            var result = new List<MyClubItemDto>();
            foreach (var (club, role) in list)
            {
                result.Add(new MyClubItemDto
                {
                    ClubId = club.Id,
                    Name = club.Name,
                    SubName = club.SubName,
                    LogoUrl = club.LogoUrl,
                    CategoryName = club.Category.Name,
                    IsActive = club.IsActive,
                    RoleInClub = role,
                    IsManager = string.Equals(role, "President", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase)
                });
            }
            return result.OrderBy(c => c.Name).ToList();
        }

        public async Task<List<ClubMemberItemDto>> GetClubMembersAsync(int clubId)
        {
            var raw = await _repo.GetClubMembersAsync(clubId);
            return raw.Select(x => new ClubMemberItemDto
            {
                StudentId = x.StudentId,
                FullName = x.FullName,
                RoleInClub = x.RoleInClub,
                IsActive = x.IsActive,
                JoinedAt = x.JoinedAt
            }).OrderBy(m => m.FullName).ToList();
        }

        public Task<bool> LeaveClubAsync(int userId, int clubId)
            => _repo.LeaveClubAsync(userId, clubId);

        public async Task<List<ClubMemberManageItemDto>> GetMembersForManageAsync(int clubId)
        {
            var list = await _repo.GetMembersForManageAsync(clubId);
            return list.Select(x => new ClubMemberManageItemDto
            {
                Id = x.Id,
                StudentId = x.StudentId,
                FullName = x.FullName,
                RoleInClub = x.RoleInClub,
                IsActive = x.IsActive,
                JoinedAt = x.JoinedAt
            }).ToList();
        }

        public Task<bool> UpdateMemberRoleAsync(int clubId, int studentId, string role)
            => _repo.UpdateMemberRoleAsync(clubId, studentId, role);

        public Task<bool> RemoveMemberAsync(int clubId, int studentId)
            => _repo.RemoveMemberAsync(clubId, studentId);

        public Task<bool> UpdateClubInfoAsync(int clubId, UpdateClubInfoDto dto)
            => _repo.UpdateClubInfoAsync(clubId, dto.Name, dto.SubName, dto.Description, dto.LogoUrl, dto.BannerUrl, dto.CategoryId, dto.IsActive);

        public async Task<List<CategoryItemDto>> GetAllCategoriesAsyncLite()
        {
            var list = await _repo.GetAllCategoriesAsyncLite();
            return list.Select(x => new CategoryItemDto { Id = x.Id, Name = x.Name }).ToList();
        }
    }
}
