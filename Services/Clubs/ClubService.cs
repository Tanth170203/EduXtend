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
    }
}
