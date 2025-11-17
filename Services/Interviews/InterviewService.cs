using BusinessObject.DTOs.Interview;
using BusinessObject.Models;
using Repositories.Interviews;
using Repositories.JoinRequests;
using Repositories.Notifications;
using Microsoft.Extensions.Logging;
using Utils;

namespace Services.Interviews
{
    public class InterviewService : IInterviewService
    {
        private readonly IInterviewRepository _interviewRepo;
        private readonly IJoinRequestRepository _joinRequestRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly ILogger<InterviewService> _logger;

        public InterviewService(
            IInterviewRepository interviewRepo, 
            IJoinRequestRepository joinRequestRepo,
            INotificationRepository notificationRepo,
            ILogger<InterviewService> logger)
        {
            _interviewRepo = interviewRepo;
            _joinRequestRepo = joinRequestRepo;
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        public async Task<InterviewDto?> GetByIdAsync(int id)
        {
            var interview = await _interviewRepo.GetByIdAsync(id);
            return interview != null ? MapToDto(interview, showEvaluationForManager: true) : null;
        }

        public async Task<InterviewDto?> GetByJoinRequestIdAsync(int joinRequestId)
        {
            var interview = await _interviewRepo.GetByJoinRequestIdAsync(joinRequestId);
            return interview != null ? MapToDto(interview, showEvaluationForManager: true) : null;
        }

        public async Task<List<InterviewDto>> GetMyInterviewsAsync(int userId)
        {
            var interviews = await _interviewRepo.GetByUserIdAsync(userId);
            return interviews.Select(i => MapToDto(i, showEvaluationForManager: false)).ToList();
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
                CreatedAt = DateTimeHelper.Now
            };

            var created = await _interviewRepo.CreateAsync(interview);
            
            // Create notification for student
            try
            {
                var notification = new Notification
                {
                    Title = "Lịch phỏng vấn mới",
                    Message = $"Bạn có lịch phỏng vấn cho câu lạc bộ {joinRequest.Club?.Name} vào {dto.ScheduledDate:dd/MM/yyyy HH:mm} tại {dto.Location}",
                    Scope = "User",
                    TargetUserId = joinRequest.UserId,
                    CreatedById = createdById,
                    IsRead = false,
                    CreatedAt = DateTimeHelper.Now
                };
                await _notificationRepo.CreateAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for interview {InterviewId}", created.Id);
            }
            
            // Reload with includes
            var result = await _interviewRepo.GetByIdAsync(created.Id);
            return MapToDto(result!, showEvaluationForManager: true);
        }

        public async Task<InterviewDto> UpdateInterviewAsync(int id, UpdateInterviewDto dto)
        {
            var interview = await _interviewRepo.GetByIdAsync(id);
            if (interview == null)
                throw new Exception("Interview not found");

            interview.ScheduledDate = dto.ScheduledDate;
            interview.Location = dto.Location;
            interview.Notes = dto.Notes;

            await _interviewRepo.UpdateAsync(interview);

            // Create notification for student about the update
            try
            {
                var notification = new Notification
                {
                    Title = "Lịch phỏng vấn đã được cập nhật",
                    Message = $"Lịch phỏng vấn của bạn cho câu lạc bộ {interview.JoinRequest.Club.Name} đã được cập nhật. Thời gian mới: {dto.ScheduledDate:dd/MM/yyyy HH:mm}, Địa điểm: {dto.Location}",
                    Scope = "User",
                    TargetUserId = interview.JoinRequest.UserId,
                    CreatedById = interview.CreatedById,
                    IsRead = false,
                    CreatedAt = DateTimeHelper.Now
                };
                await _notificationRepo.CreateAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for interview update {InterviewId}", id);
            }

            // Reload with includes
            var result = await _interviewRepo.GetByIdAsync(id);
            return MapToDto(result!, showEvaluationForManager: true);
        }

        public async Task<InterviewDto> UpdateEvaluationAsync(int id, UpdateEvaluationDto dto)
        {
            var interview = await _interviewRepo.GetByIdAsync(id);
            if (interview == null)
                throw new Exception("Interview not found");

            interview.Evaluation = dto.Evaluation;
            interview.Status = "Completed";
            interview.CompletedAt = DateTimeHelper.Now;

            await _interviewRepo.UpdateAsync(interview);

            // Reload with includes
            var result = await _interviewRepo.GetByIdAsync(id);
            return MapToDto(result!, showEvaluationForManager: true);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _interviewRepo.DeleteAsync(id);
        }

        private InterviewDto MapToDto(Interview interview, bool showEvaluationForManager = false)
        {
            // Show evaluation if:
            // 1. Manager is viewing (showEvaluationForManager = true), OR
            // 2. Join request has been processed (Approved/Rejected) - for student view
            var joinRequestStatus = interview.JoinRequest.Status;
            var showEvaluation = showEvaluationForManager || 
                                 joinRequestStatus == "Approved" || 
                                 joinRequestStatus == "Rejected";
            
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
                Evaluation = showEvaluation ? interview.Evaluation : null,
                Status = interview.Status,
                CreatedAt = interview.CreatedAt,
                CompletedAt = interview.CompletedAt,
                CreatedById = interview.CreatedById,
                CreatedByName = interview.CreatedBy.FullName
            };
        }
    }
}

