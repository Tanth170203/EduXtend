using BusinessObject.DTOs.Semester;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Semesters
{
    public interface ISemesterService
    {
        Task<IEnumerable<SemesterDto>> GetAllAsync();
        Task<SemesterDto?> GetByIdAsync(int id);
        Task<SemesterDto> CreateAsync(CreateSemesterDto dto);
        Task<SemesterDto> UpdateAsync(int id, UpdateSemesterDto dto);
        Task<bool> DeleteAsync(int id);
        Task<SemesterDto?> GetActiveAsync();
        Task UpdateAllActiveStatusAsync();
        Task<IEnumerable<SemesterDto>> GetOverlappingSemestersAsync(DateTime startDate, DateTime endDate, int? excludeId = null);
    }
}

