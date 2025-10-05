using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.AcademicYears
{
    public interface IAcademicYearRepository
    {
        Task<IEnumerable<AcademicYear>> GetAllAsync();
        Task<AcademicYear?> GetByIdAsync(int id);
        Task<AcademicYear> CreateAsync(AcademicYear academicYear);
        Task<AcademicYear> UpdateAsync(AcademicYear academicYear);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<AcademicYear?> GetActiveAsync();
    }
}
