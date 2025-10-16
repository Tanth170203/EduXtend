using BusinessObject.DTOs.Semester;
using BusinessObject.Models;
using Repositories.Semesters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Semesters
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _repository;

        public SemesterService(ISemesterRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SemesterDto>> GetAllAsync()
        {
            var semesters = await _repository.GetAllAsync();
            return semesters.Select(MapToDto);
        }

        public async Task<SemesterDto?> GetByIdAsync(int id)
        {
            var semester = await _repository.GetByIdAsync(id);
            return semester != null ? MapToDto(semester) : null;
        }

        public async Task<SemesterDto> CreateAsync(CreateSemesterDto dto)
        {
            // Validate business rules
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");

            // Kiểm tra overlap với các học kỳ khác (nếu không force)
            if (!dto.Force)
            {
                var existingSemesters = await _repository.GetAllAsync();
                var overlappingSemesters = existingSemesters
                    .Where(s => dto.StartDate.Date <= s.EndDate.Date && dto.EndDate.Date >= s.StartDate.Date)
                    .ToList();

                if (overlappingSemesters.Any())
                {
                    var overlappingNames = string.Join(", ", overlappingSemesters.Select(s => s.Name));
                    var warningMessage = $"⚠️ This semester overlaps with: {overlappingNames}";
                    throw new InvalidOperationException($"WARNING:{warningMessage}");
                }
            }

            // Tự động tính toán IsActive dựa trên ngày hiện tại
            var now = DateTime.UtcNow.Date;
            var isActive = now >= dto.StartDate.Date && now <= dto.EndDate.Date;

            var semester = new Semester
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = isActive  // Tự động set
            };

            var created = await _repository.CreateAsync(semester);
            return MapToDto(created);
        }

        public async Task<SemesterDto> UpdateAsync(int id, UpdateSemesterDto dto)
        {
            var semester = await _repository.GetByIdAsync(id);
            if (semester == null)
                throw new ArgumentException("Semester does not exist.");

            // Validate business rules
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");

            // Kiểm tra overlap với các học kỳ khác (trừ chính nó, nếu không force)
            if (!dto.Force)
            {
                var existingSemesters = await _repository.GetAllAsync();
                var overlappingSemesters = existingSemesters
                    .Where(s => s.Id != id) // Loại trừ chính nó
                    .Where(s => dto.StartDate.Date <= s.EndDate.Date && dto.EndDate.Date >= s.StartDate.Date)
                    .ToList();

                if (overlappingSemesters.Any())
                {
                    var overlappingNames = string.Join(", ", overlappingSemesters.Select(s => s.Name));
                    var warningMessage = $"⚠️ This semester overlaps with: {overlappingNames}";
                    throw new InvalidOperationException($"WARNING:{warningMessage}");
                }
            }

            // Tự động tính toán IsActive dựa trên ngày hiện tại
            var now = DateTime.UtcNow.Date;
            var isActive = now >= dto.StartDate.Date && now <= dto.EndDate.Date;

            semester.Name = dto.Name;
            semester.StartDate = dto.StartDate;
            semester.EndDate = dto.EndDate;
            semester.IsActive = isActive;  // Tự động set

            var updated = await _repository.UpdateAsync(semester);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // Check if the semester exists
            var semester = await _repository.GetByIdAsync(id);
            if (semester == null)
                throw new ArgumentException("Semester does not exist.");

            // Check if there are any related data
            var hasRelatedData = await _repository.HasRelatedDataAsync(id);
            if (hasRelatedData)
                throw new InvalidOperationException("Cannot delete this semester because it has related data. Please delete related data first.");

            return await _repository.DeleteAsync(id);
        }

        public async Task<SemesterDto?> GetActiveAsync()
        {
            var semester = await _repository.GetActiveAsync();
            return semester != null ? MapToDto(semester) : null;
        }

        public async Task UpdateAllActiveStatusAsync()
        {
            var semesters = await _repository.GetAllAsync();
            var now = DateTime.UtcNow.Date;

            foreach (var semester in semesters)
            {
                var shouldBeActive = now >= semester.StartDate.Date && now <= semester.EndDate.Date;

                if (semester.IsActive != shouldBeActive)
                {
                    semester.IsActive = shouldBeActive;
                    await _repository.UpdateAsync(semester);
                }
            }
        }

        public async Task<IEnumerable<SemesterDto>> GetOverlappingSemestersAsync(DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            var allSemesters = await _repository.GetAllAsync();
            
            var overlappingSemesters = allSemesters
                .Where(s => excludeId == null || s.Id != excludeId.Value)
                .Where(s => startDate.Date <= s.EndDate.Date && endDate.Date >= s.StartDate.Date)
                .Select(MapToDto)
                .ToList();

            return overlappingSemesters;
        }

        private static SemesterDto MapToDto(Semester semester)
        {
            return new SemesterDto
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                IsActive = semester.IsActive
            };
        }
    }
}

