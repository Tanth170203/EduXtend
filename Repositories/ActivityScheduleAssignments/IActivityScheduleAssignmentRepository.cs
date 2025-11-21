using BusinessObject.Models;

namespace Repositories.ActivityScheduleAssignments
{
    public interface IActivityScheduleAssignmentRepository
    {
        Task<ActivityScheduleAssignment?> GetByIdAsync(int id);
        Task<List<ActivityScheduleAssignment>> GetByScheduleIdAsync(int scheduleId);
        Task<ActivityScheduleAssignment> AddAsync(ActivityScheduleAssignment assignment);
        Task UpdateAsync(ActivityScheduleAssignment assignment);
        Task DeleteAsync(int id);
    }
}
