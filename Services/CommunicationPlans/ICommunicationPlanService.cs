using BusinessObject.DTOs.CommunicationPlan;

namespace Services.CommunicationPlans
{
    public interface ICommunicationPlanService
    {
        Task<CommunicationPlanDto> CreatePlanAsync(int userId, CreateCommunicationPlanDto dto);
        Task<CommunicationPlanDto> UpdatePlanAsync(int userId, int planId, UpdateCommunicationPlanDto dto);
        Task<CommunicationPlanDto> GetPlanAsync(int userId, int planId);
        Task<List<CommunicationPlanDto>> GetClubPlansAsync(int userId, int clubId);
        Task<bool> DeletePlanAsync(int userId, int planId);
        Task<List<AvailableActivityDto>> GetAvailableActivitiesAsync(int userId);
    }

    public class AvailableActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime StartTime { get; set; }
    }
}
