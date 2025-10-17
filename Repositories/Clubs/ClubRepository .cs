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

        public async Task<Club?> GetByIdAsync(int id)
            => await _ctx.Clubs
                .AsNoTracking()
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);
    }
}
