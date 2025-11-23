using BusinessObject.Models;

namespace Repositories.ActivitySchedules
{
    public interface IActivityScheduleRepository
    {
        Task<ActivitySchedule?> GetByIdAsync(int id);
        Task<List<ActivitySchedule>> GetByActivityIdAsync(int activityId);
        Task<ActivitySchedule> AddAsync(ActivitySchedule schedule);
        Task UpdateAsync(ActivitySchedule schedule);
        Task DeleteAsync(int id);
    }
}
