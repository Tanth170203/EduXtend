using BusinessObject.DTOs.Interview;

namespace Services.Interviews
{
    public interface IInterviewService
    {
        Task<InterviewDto?> GetByIdAsync(int id);
        Task<InterviewDto?> GetByJoinRequestIdAsync(int joinRequestId);
        Task<List<InterviewDto>> GetMyInterviewsAsync(int userId);
        Task<InterviewDto> ScheduleInterviewAsync(ScheduleInterviewDto dto, int createdById);
        Task<InterviewDto> UpdateInterviewAsync(int id, UpdateInterviewDto dto);
        Task<InterviewDto> UpdateEvaluationAsync(int id, UpdateEvaluationDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

