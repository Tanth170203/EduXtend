using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.AcademicYears
{
    public class AcademicYearRepository : IAcademicYearRepository
    {
        private readonly EduXtendContext _context;

        public AcademicYearRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AcademicYear>> GetAllAsync()
        {
            return await _context.AcademicYears
                .OrderByDescending(ay => ay.StartDate)
                .ToListAsync();
        }

        public async Task<AcademicYear?> GetByIdAsync(int id)
        {
            return await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.Id == id);
        }

        public async Task<AcademicYear> CreateAsync(AcademicYear academicYear)
        {
            _context.AcademicYears.Add(academicYear);
            await _context.SaveChangesAsync();
            return academicYear;
        }

        public async Task<AcademicYear> UpdateAsync(AcademicYear academicYear)
        {
            _context.AcademicYears.Update(academicYear);
            await _context.SaveChangesAsync();
            return academicYear;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var academicYear = await _context.AcademicYears.FindAsync(id);
            if (academicYear == null)
                return false;

            _context.AcademicYears.Remove(academicYear);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.AcademicYears.AnyAsync(ay => ay.Id == id);
        }

        public async Task<AcademicYear?> GetActiveAsync()
        {
            return await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.IsActive);
        }
    }
}
