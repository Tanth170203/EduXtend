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

        public async Task<List<Club>> GetAllManagedClubsByUserIdAsync(int userId)
        {
            // Find all clubs where user is President or Manager
            var clubIds = await _ctx.ClubMembers
                .AsNoTracking()
                .Include(cm => cm.Student)
                .Where(cm => cm.Student.UserId == userId 
                    && cm.IsActive
                    && (cm.RoleInClub == "President" || cm.RoleInClub == "Manager"))
                .Select(cm => cm.ClubId)
                .Distinct()
                .ToListAsync();

            // Load clubs with necessary includes
            var clubs = await _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.Members)
                .Include(c => c.Activities)
                .Where(c => clubIds.Contains(c.Id))
                .ToListAsync();

            return clubs;
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

        public async Task<bool> IsUserMemberOfClubAsync(int userId, int clubId)
        {
            // First get student from userId
            var student = await _ctx.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null) return false;

            // Check if student is an active member of the club
            return await _ctx.ClubMembers
                .AnyAsync(cm => cm.StudentId == student.Id && cm.ClubId == clubId && cm.IsActive);
        }

        public async Task<List<Club>> GetClubsByUserIdAsync(int userId)
        {
            // First get student from userId
            var student = await _ctx.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null) return new List<Club>();

            // Get all clubs where student is an active member
            return await _ctx.ClubMembers
                .AsNoTracking()
                .Where(cm => cm.StudentId == student.Id && cm.IsActive)
                .Include(cm => cm.Club)
                    .ThenInclude(c => c.Category)
                .Select(cm => cm.Club)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<string?> GetUserRoleInClubAsync(int userId, int clubId)
        {
            var student = await _ctx.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null) return null;

            var member = await _ctx.ClubMembers
                .AsNoTracking()
                .Where(cm => cm.StudentId == student.Id && cm.ClubId == clubId && cm.IsActive)
                .FirstOrDefaultAsync();

            return member?.RoleInClub;
        }

        public async Task<List<ClubMember>> GetClubMembersAsync(int clubId)
        {
            return await _ctx.ClubMembers
                .AsNoTracking()
                .Where(cm => cm.ClubId == clubId && cm.IsActive)
                .Include(cm => cm.Student)
                    .ThenInclude(s => s.User)
                .Include(cm => cm.Department)
                .OrderBy(cm => cm.RoleInClub)
                .ThenBy(cm => cm.Student.User.FullName)
                .ToListAsync();
        }

        public async Task<List<ClubDepartment>> GetClubDepartmentsAsync(int clubId)
        {
            return await _ctx.ClubDepartments
                .AsNoTracking()
                .Where(cd => cd.ClubId == clubId)
                .Include(cd => cd.Members.Where(m => m.IsActive))
                .OrderBy(cd => cd.Name)
                .ToListAsync();
        }

        public async Task<List<ClubAward>> GetClubAwardsAsync(int clubId)
        {
            return await _ctx.ClubAwards
                .AsNoTracking()
                .Where(ca => ca.ClubId == clubId)
                .Include(ca => ca.Semester)
                .OrderByDescending(ca => ca.AwardedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Club club)
        {
            _ctx.Clubs.Update(club);
            await _ctx.SaveChangesAsync();
        }
    }
}

