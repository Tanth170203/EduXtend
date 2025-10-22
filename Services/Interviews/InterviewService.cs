using BusinessObject.DTOs.Interview;
using BusinessObject.Models;
using Repositories.Interviews;
using Repositories.JoinRequests;

namespace Services.Interviews
{
    public class InterviewService : IInterviewService
    {
        private readonly IInterviewRepository _interviewRepo;
        private readonly IJoinRequestRepository _joinRequestRepo;

        public InterviewService(IInterviewRepository interviewRepo, IJoinRequestRepository joinRequestRepo)
        {
            _interviewRepo = interviewRepo;
            _joinRequestRepo = joinRequestRepo;
        }

        public async Task<InterviewDto?> GetByIdAsync(int id)
        {
            var interview = await _interviewRepo.GetByIdAsync(id);
            return interview != null ? MapToDto(interview) : null;
        }

        public async Task<InterviewDto?> GetByJoinRequestIdAsync(int joinRequestId)
        {
            var interview = await _interviewRepo.GetByJoinRequestIdAsync(joinRequestId);
            return interview != null ? MapToDto(interview) : null;
        }

        public async Task<List<InterviewDto>> GetMyInterviewsAsync(int userId)
        {
            var interviews = await _interviewRepo.GetByUserIdAsync(userId);
            return interviews.Select(MapToDto).ToList();
        }

        public async Task<InterviewDto> ScheduleInterviewAsync(ScheduleInterviewDto dto, int createdById)
        {
            // Validate join request exists and is pending
            var joinRequest = await _joinRequestRepo.GetByIdAsync(dto.JoinRequestId);
            if (joinRequest == null)
                throw new Exception("Join request not found");

            if (joinRequest.Status != "Pending")
                throw new Exception("Can only schedule interview for pending requests");

            // Check if interview already exists
            if (await _interviewRepo.ExistsForJoinRequestAsync(dto.JoinRequestId))
                throw new Exception("Interview already scheduled for this request");

            var interview = new Interview
            {
                JoinRequestId = dto.JoinRequestId,
                ScheduledDate = dto.ScheduledDate,
                Location = dto.Location,
                Notes = dto.Notes,
                Status = "Scheduled",
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _interviewRepo.CreateAsync(interview);
            
            // Reload with includes
            var result = await _interviewRepo.GetByIdAsync(created.Id);
            return MapToDto(result!);
        }

        public async Task<InterviewDto> UpdateEvaluationAsync(int id, UpdateEvaluationDto dto)
        {
            var interview = await _interviewRepo.GetByIdAsync(id);
            if (interview == null)
                throw new Exception("Interview not found");

            interview.Evaluation = dto.Evaluation;
            interview.Status = "Completed";
            interview.CompletedAt = DateTime.UtcNow;

            await _interviewRepo.UpdateAsync(interview);

            // Reload with includes
            var result = await _interviewRepo.GetByIdAsync(id);
            return MapToDto(result!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _interviewRepo.DeleteAsync(id);
        }

        private InterviewDto MapToDto(Interview interview)
        {
            return new InterviewDto
            {
                Id = interview.Id,
                JoinRequestId = interview.JoinRequestId,
                ClubId = interview.JoinRequest.ClubId,
                ClubName = interview.JoinRequest.Club.Name,
                UserId = interview.JoinRequest.UserId,
                UserName = interview.JoinRequest.User.FullName,
                UserEmail = interview.JoinRequest.User.Email,
                ScheduledDate = interview.ScheduledDate,
                Location = interview.Location,
                Notes = interview.Notes,
                Evaluation = interview.Evaluation,
                Status = interview.Status,
                CreatedAt = interview.CreatedAt,
                CompletedAt = interview.CompletedAt,
                CreatedById = interview.CreatedById,
                CreatedByName = interview.CreatedBy.FullName
            };
        }
    }
}

