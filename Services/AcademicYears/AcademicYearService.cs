using BusinessObject.DTOs.AcademicYear;
using BusinessObject.Models;
using Repositories.AcademicYears;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.AcademicYears
{
    public class AcademicYearService : IAcademicYearService
    {
        private readonly IAcademicYearRepository _repository;

        public AcademicYearService(IAcademicYearRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AcademicYearDto>> GetAllAsync()
        {
            var academicYears = await _repository.GetAllAsync();
            return academicYears.Select(MapToDto);
        }

        public async Task<AcademicYearDto?> GetByIdAsync(int id)
        {
            var academicYear = await _repository.GetByIdAsync(id);
            return academicYear != null ? MapToDto(academicYear) : null;
        }

        public async Task<AcademicYearDto> CreateAsync(CreateAcademicYearDto dto)
        {
            // Validate business rules
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");

            // If setting as active, deactivate others
            if (dto.IsActive)
            {
                var activeYear = await _repository.GetActiveAsync();
                if (activeYear != null)
                {
                    activeYear.IsActive = false;
                    await _repository.UpdateAsync(activeYear);
                }
            }

            var academicYear = new AcademicYear
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive
            };

            var created = await _repository.CreateAsync(academicYear);
            return MapToDto(created);
        }

        public async Task<AcademicYearDto> UpdateAsync(int id, UpdateAcademicYearDto dto)
        {
            var academicYear = await _repository.GetByIdAsync(id);
            if (academicYear == null)
                throw new ArgumentException("Academic year not found.");

            // Validate business rules
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");

            // If setting as active, deactivate others
            if (dto.IsActive && !academicYear.IsActive)
            {
                var activeYear = await _repository.GetActiveAsync();
                if (activeYear != null && activeYear.Id != id)
                {
                    activeYear.IsActive = false;
                    await _repository.UpdateAsync(activeYear);
                }
            }

            academicYear.Name = dto.Name;
            academicYear.StartDate = dto.StartDate;
            academicYear.EndDate = dto.EndDate;
            academicYear.IsActive = dto.IsActive;

            var updated = await _repository.UpdateAsync(academicYear);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<AcademicYearDto?> GetActiveAsync()
        {
            var academicYear = await _repository.GetActiveAsync();
            return academicYear != null ? MapToDto(academicYear) : null;
        }

        private static AcademicYearDto MapToDto(AcademicYear academicYear)
        {
            return new AcademicYearDto
            {
                Id = academicYear.Id,
                Name = academicYear.Name,
                StartDate = academicYear.StartDate,
                EndDate = academicYear.EndDate,
                IsActive = academicYear.IsActive
            };
        }
    }
}
