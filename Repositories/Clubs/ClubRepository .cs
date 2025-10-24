using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Clubs
{
   public class ClubRepository : IClubRepository
    {
        private readonly EduXtendContext _ctx;
        public ClubRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<List<Club>> GetAllAsync()
            => await _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<List<Club>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive)
        {
            var query = _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => 
                    c.Name.Contains(searchTerm) || 
                    c.SubName.Contains(searchTerm) ||
                    (c.Description != null && c.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Where(c => c.Category.Name.Contains(categoryName));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<List<ClubCategory>> GetAllCategoriesAsync()
            => await _ctx.ClubCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<Club?> GetByIdAsync(int id)
            => await _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Club?> GetByIdWithDetailsAsync(int id)
            => await _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.Members.Where(m => m.IsActive))
                    .ThenInclude(m => m.Student)
                .Include(c => c.Activities)
                .Include(c => c.Departments)
                .Include(c => c.Awards)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<int> GetMemberCountAsync(int clubId)
            => await _ctx.ClubMembers
                .Where(m => m.ClubId == clubId && m.IsActive)
                .CountAsync();

        public async Task<int> GetActivityCountAsync(int clubId)
            => await _ctx.Activities
                .Where(a => a.ClubId == clubId)
                .CountAsync();

        public async Task<Club?> GetManagedClubByUserIdAsync(int userId)
        {
            // Find club where user is President or Manager
            var club = await _ctx.ClubMembers
                .AsNoTracking()
                .Include(cm => cm.Student)
                .Include(cm => cm.Club)
                    .ThenInclude(c => c.Category)
                .Where(cm => cm.Student.UserId == userId 
                    && cm.IsActive
                    && (cm.RoleInClub == "President" || cm.RoleInClub == "Manager"))
                .Select(cm => cm.Club)
                .FirstOrDefaultAsync();

            return club;
        }

        public async Task<bool> ToggleRecruitmentAsync(int clubId, bool isOpen)
        {
            var club = await _ctx.Clubs.FindAsync(clubId);
            if (club == null) return false;

            club.IsRecruitmentOpen = isOpen;
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetPendingRequestCountAsync(int clubId)
            => await _ctx.JoinRequests
                .Where(jr => jr.ClubId == clubId && jr.Status == "Pending")
                .CountAsync();

        public async Task<List<(int Id, string Name)>> GetAllCategoriesAsyncLite()
        {
            var list = await _ctx.ClubCategories.AsNoTracking().OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToListAsync();
            return list.Select(x => (x.Id, x.Name)).ToList();
        }

        public async Task<List<(Club Club, string RoleInClub)>> GetClubsByUserIdAsync(int userId)
        {
            // 1) Get memberships for this user (Student -> ClubMember)
            var memberships = await (from cm in _ctx.ClubMembers.AsNoTracking()
                                     join s in _ctx.Students.AsNoTracking() on cm.StudentId equals s.Id
                                     where s.UserId == userId && cm.IsActive
                                     select new { cm.ClubId, cm.RoleInClub })
                                    .ToListAsync();

            if (memberships.Count == 0) return new List<(Club, string)>();

            var clubIds = memberships.Select(m => m.ClubId).Distinct().ToList();

            // 2) Load clubs with category
            var clubs = await _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .Where(c => clubIds.Contains(c.Id))
                .ToListAsync();

            // 3) Join in-memory to return pairs
            var roleByClubId = memberships
                .GroupBy(m => m.ClubId)
                .ToDictionary(g => g.Key, g => g.First().RoleInClub);

            var result = new List<(Club, string)>();
            foreach (var club in clubs)
            {
                if (roleByClubId.TryGetValue(club.Id, out var role))
                {
                    result.Add((club, role));
                }
            }

            return result;
        }

        public async Task<List<(int StudentId, string FullName, string RoleInClub, bool IsActive, DateTime JoinedAt)>> GetClubMembersAsync(int clubId)
        {
            var query = from cm in _ctx.ClubMembers.AsNoTracking()
                        join s in _ctx.Students.AsNoTracking() on cm.StudentId equals s.Id
                        where cm.ClubId == clubId
                        orderby s.FullName
                        select new { cm.StudentId, s.FullName, cm.RoleInClub, cm.IsActive, cm.JoinedAt };

            var list = await query.ToListAsync();
            return list.Select(x => (x.StudentId, x.FullName, x.RoleInClub, x.IsActive, x.JoinedAt)).ToList();
        }

        public async Task<bool> LeaveClubAsync(int userId, int clubId)
        {
            // map user -> student
            var studentId = await _ctx.Students
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
            if (studentId == 0) return false;

            var membership = await _ctx.ClubMembers
                .FirstOrDefaultAsync(cm => cm.ClubId == clubId && cm.StudentId == studentId && cm.IsActive);
            if (membership == null) return false;

            membership.IsActive = false;
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<List<(int Id, int StudentId, string FullName, string RoleInClub, bool IsActive, DateTime JoinedAt)>> GetMembersForManageAsync(int clubId)
        {
            var query = from cm in _ctx.ClubMembers.AsNoTracking()
                        join s in _ctx.Students.AsNoTracking() on cm.StudentId equals s.Id
                        where cm.ClubId == clubId
                        orderby s.FullName
                        select new { cm.Id, cm.StudentId, s.FullName, cm.RoleInClub, cm.IsActive, cm.JoinedAt };
            var list = await query.ToListAsync();
            return list.Select(x => (x.Id, x.StudentId, x.FullName, x.RoleInClub, x.IsActive, x.JoinedAt)).ToList();
        }

        public async Task<bool> UpdateMemberRoleAsync(int clubId, int studentId, string role)
        {
            var member = await _ctx.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.StudentId == studentId);
            if (member == null) return false;
            member.RoleInClub = role;
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(int clubId, int studentId)
        {
            var member = await _ctx.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.StudentId == studentId);
            if (member == null) return false;
            _ctx.ClubMembers.Remove(member);
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateClubInfoAsync(int clubId, string? name, string? subName, string? description, string? logoUrl, string? bannerUrl, int? categoryId, bool? isActive)
        {
            var club = await _ctx.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
            if (club == null) return false;
            if (!string.IsNullOrWhiteSpace(name)) club.Name = name;
            if (subName != null) club.SubName = subName;
            if (description != null) club.Description = description;
            if (logoUrl != null) club.LogoUrl = logoUrl;
            if (bannerUrl != null) club.BannerUrl = bannerUrl;
            if (categoryId.HasValue) club.CategoryId = categoryId.Value;
            if (isActive.HasValue) club.IsActive = isActive.Value;
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
