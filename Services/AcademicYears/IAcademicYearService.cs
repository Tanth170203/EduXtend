using BusinessObject.DTOs.AcademicYear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.AcademicYears
{
    public interface IAcademicYearService
    {
        Task<IEnumerable<AcademicYearDto>> GetAllAsync();
        Task<AcademicYearDto?> GetByIdAsync(int id);
        Task<AcademicYearDto> CreateAsync(CreateAcademicYearDto dto);
        Task<AcademicYearDto> UpdateAsync(int id, UpdateAcademicYearDto dto);
        Task<bool> DeleteAsync(int id);
        Task<AcademicYearDto?> GetActiveAsync();
    }
}
