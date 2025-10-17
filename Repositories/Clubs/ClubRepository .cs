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
    }
}
