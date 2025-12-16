using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.JoinRequest;
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

        public async Task<List<ClubListItemDto>> GetAllClubsAsync(int? userId = null)
        {
            var clubs = await _repo.GetAllAsync();
            var result = new List<ClubListItemDto>();

            foreach (var c in clubs)
            {
                bool isMember = false;
                if (userId.HasValue)
                {
                    isMember = await _repo.IsUserMemberOfClubAsync(userId.Value, c.Id);
                }

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
                    ActivityCount = await _repo.GetActivityCountAsync(c.Id),
                    IsMember = isMember
                });
            }

            return result;
        }

        public async Task<List<ClubListItemDto>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive, int? userId = null)
        {
            var clubs = await _repo.SearchClubsAsync(searchTerm, categoryName, isActive);
            var result = new List<ClubListItemDto>();

            foreach (var c in clubs)
            {
                bool isMember = false;
                if (userId.HasValue)
                {
                    isMember = await _repo.IsUserMemberOfClubAsync(userId.Value, c.Id);
                }

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
                    ActivityCount = await _repo.GetActivityCountAsync(c.Id),
                    IsMember = isMember
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

        public async Task<List<ClubListItemDto>> GetAllManagedClubsByUserIdAsync(int userId)
        {
            var clubs = await _repo.GetAllManagedClubsByUserIdAsync(userId);
            
            return clubs.Select(c => new ClubListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                SubName = c.SubName,
                Description = c.Description,
                LogoUrl = c.LogoUrl,
                IsActive = c.IsActive,
                IsRecruitmentOpen = c.IsRecruitmentOpen,
                FoundedDate = c.FoundedDate,
                CategoryName = c.Category?.Name ?? "Unknown",
                MemberCount = c.Members?.Count(m => m.IsActive) ?? 0,
                ActivityCount = c.Activities?.Count ?? 0
            }).ToList();
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

        public async Task<bool> IsUserMemberOfClubAsync(int userId, int clubId)
        {
            return await _repo.IsUserMemberOfClubAsync(userId, clubId);
        }

        public async Task<List<ClubListItemDto>> GetClubsByUserIdAsync(int userId)
        {
            var clubs = await _repo.GetClubsByUserIdAsync(userId);
            var result = new List<ClubListItemDto>();

            foreach (var c in clubs)
            {
                var role = await _repo.GetUserRoleInClubAsync(userId, c.Id);
                var isManager = role == "Manager"; // Chỉ Manager, không phải President

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
                    ActivityCount = await _repo.GetActivityCountAsync(c.Id),
                    IsManager = isManager
                });
            }

            return result;
        }

        public async Task<List<ClubMemberDto>> GetClubMembersAsync(int clubId)
        {
            var members = await _repo.GetClubMembersAsync(clubId);
            return members.Select(m => new ClubMemberDto
            {
                Id = m.Id,
                StudentId = m.StudentId,
                StudentName = m.Student.User.FullName ?? "Unknown",
                StudentCode = m.Student.StudentCode,
                Email = m.Student.User.Email ?? "",
                AvatarUrl = m.Student.User.AvatarUrl,
                RoleInClub = m.RoleInClub,
                DepartmentId = m.DepartmentId,
                DepartmentName = m.Department?.Name,
                JoinedAt = m.JoinedAt,
                IsActive = m.IsActive
            }).ToList();
        }

        public async Task<List<DepartmentDto>> GetClubDepartmentsAsync(int clubId)
        {
            var departments = await _repo.GetClubDepartmentsAsync(clubId);
            return departments.Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                MemberCount = d.Members.Count(m => m.IsActive && m.DepartmentId == d.Id)
            }).ToList();
        }

        public async Task<List<ClubAwardDto>> GetClubAwardsAsync(int clubId)
        {
            var awards = await _repo.GetClubAwardsAsync(clubId);
            return awards.Select(a => new ClubAwardDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description,
                SemesterId = a.SemesterId,
                SemesterName = a.Semester?.Name,
                AwardedAt = a.AwardedAt
            }).ToList();
        }

        public async Task<ClubDetailDto?> UpdateClubInfoAsync(int clubId, UpdateClubInfoDto dto)
        {
            var club = await _repo.GetByIdAsync(clubId);
            if (club == null) return null;

            // Update fields
            club.Name = dto.Name;
            club.SubName = dto.SubName;
            club.Description = dto.Description;
            club.LogoUrl = dto.LogoUrl;
            club.BannerUrl = dto.BannerUrl;

            await _repo.UpdateAsync(club);

            // Return updated club detail
            return await GetClubByIdAsync(clubId);
        }
    }
}
