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
    }
}
