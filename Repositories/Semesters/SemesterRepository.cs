using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Semesters
{
    public class SemesterRepository : ISemesterRepository
    {
        private readonly EduXtendContext _context;

        public SemesterRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Semester>> GetAllAsync()
        {
            return await _context.Semesters
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<Semester?> GetByIdAsync(int id)
        {
            return await _context.Semesters
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Semester> CreateAsync(Semester semester)
        {
            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();
            return semester;
        }

        public async Task<Semester> UpdateAsync(Semester semester)
        {
            _context.Semesters.Update(semester);
            await _context.SaveChangesAsync();
            return semester;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null)
                return false;

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Semesters.AnyAsync(s => s.Id == id);
        }

        public async Task<Semester?> GetActiveAsync()
        {
            return await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsActive);
        }

        public async Task<bool> HasRelatedDataAsync(int id)
        {
            // Check if there are any related data (activities, movement records, etc.)
            var hasMovementRecords = await _context.MovementRecords
                .AnyAsync(mr => mr.SemesterId == id);
            
            var hasClubAwards = await _context.ClubAwards
                .AnyAsync(ca => ca.SemesterId == id);

            return hasMovementRecords || hasClubAwards;
        }
    }
}

