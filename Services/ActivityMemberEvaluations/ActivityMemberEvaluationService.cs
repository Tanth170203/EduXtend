using BusinessObject.DTOs.ActivityMemberEvaluation;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Repositories.ActivityMemberEvaluations;
using Repositories.Activities;
using Repositories.ClubMembers;
using Repositories.Users;
using Services.Notifications;
using Services.MovementRecords;

namespace Services.ActivityMemberEvaluations
{
    public class ActivityMemberEvaluationService : IActivityMemberEvaluationService
    {
        private readonly IActivityMemberEvaluationRepository _evaluationRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IUserRepository _userRepository;
        private readonly IClubMemberRepository _clubMemberRepository;
        private readonly INotificationService _notificationService;
        private readonly EduXtendContext _context;
        private readonly IMovementRecordService _movementRecordService;

        public ActivityMemberEvaluationService(
            IActivityMemberEvaluationRepository evaluationRepository,
            IActivityRepository activityRepository,
            IUserRepository userRepository,
            IClubMemberRepository clubMemberRepository,
            INotificationService notificationService,
            EduXtendContext context,
            IMovementRecordService movementRecordService)
        {
            _evaluationRepository = evaluationRepository;
            _activityRepository = activityRepository;
            _userRepository = userRepository;
            _clubMemberRepository = clubMemberRepository;
            _notificationService = notificationService;
            _context = context;
            _movementRecordService = movementRecordService;
        }

        public async Task<List<ActivityMemberEvaluationListDto>> GetAssignmentsForEvaluationAsync(int activityId)
        {
            // Lấy tất cả assignments của activity
            var assignments = await _context.ActivityScheduleAssignments
                .Include(a => a.ActivitySchedule)
                .Include(a => a.User)
                .Where(a => a.ActivitySchedule.ActivityId == activityId)
                .ToListAsync();

            // Lấy tất cả evaluations của activity
            var evaluations = await _evaluationRepository.GetByActivityIdAsync(activityId);
            var evaluationDict = evaluations.ToDictionary(e => e.ActivityScheduleAssignmentId);

            // Map to DTOs
            var result = assignments.Select(a =>
            {
                var hasEvaluation = evaluationDict.TryGetValue(a.Id, out var evaluation);
                return new ActivityMemberEvaluationListDto
                {
                    AssignmentId = a.Id,
                    UserId = a.UserId,
                    UserName = a.User?.FullName,
                    ResponsibleName = a.ResponsibleName,
                    Role = a.Role,
                    ScheduleTitle = a.ActivitySchedule.Title,
                    StartTime = a.ActivitySchedule.StartTime,
                    EndTime = a.ActivitySchedule.EndTime,
                    IsEvaluated = hasEvaluation,
                    AverageScore = hasEvaluation ? evaluation!.AverageScore : null,
                    EvaluatedAt = hasEvaluation ? evaluation!.CreatedAt : null
                };
            }).ToList();

            return result;
        }

        public async Task<ActivityMemberEvaluationDto> CreateEvaluationAsync(int evaluatorId, CreateActivityMemberEvaluationDto dto)
        {
            // Validate assignment exists
            var assignment = await _context.ActivityScheduleAssignments
                .Include(a => a.ActivitySchedule)
                    .ThenInclude(s => s.Activity)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == dto.ActivityScheduleAssignmentId);

            if (assignment == null)
            {
                throw new ArgumentException("Assignment not found");
            }

            // Validate assignment hasn't been evaluated yet
            var exists = await _evaluationRepository.ExistsAsync(dto.ActivityScheduleAssignmentId);
            if (exists)
            {
                throw new InvalidOperationException("This assignment has already been evaluated");
            }

            // Validate evaluator has permission
            var canEvaluate = await CanEvaluateAsync(evaluatorId, assignment.ActivitySchedule.ActivityId);
            if (!canEvaluate)
            {
                throw new UnauthorizedAccessException("You do not have permission to evaluate this activity");
            }

            // Validate scores
            ValidateScores(dto);

            // Validate text lengths
            ValidateTextLengths(dto);

            // Calculate average score
            var averageScore = (dto.ResponsibilityScore + dto.SkillScore + dto.AttitudeScore + dto.EffectivenessScore) / 4.0;

            // Create evaluation
            var evaluation = new ActivityMemberEvaluation
            {
                ActivityScheduleAssignmentId = dto.ActivityScheduleAssignmentId,
                EvaluatorId = evaluatorId,
                ResponsibilityScore = dto.ResponsibilityScore,
                SkillScore = dto.SkillScore,
                AttitudeScore = dto.AttitudeScore,
                EffectivenessScore = dto.EffectivenessScore,
                AverageScore = averageScore,
                Comments = dto.Comments,
                Strengths = dto.Strengths,
                Improvements = dto.Improvements,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _evaluationRepository.CreateAsync(evaluation);

            // Add movement score based on evaluation (Tiêu chí 2.2 - Đánh giá thành viên CLB)
            // AverageScore is 1-10, map directly to movement points (1-10 ĐPT)
            if (assignment.UserId.HasValue)
            {
                try
                {
                    var movementPoints = Math.Round(created.AverageScore, 1); // Round to 1 decimal
                    await _movementRecordService.AddScoreFromEvaluationAsync(
                        studentId: assignment.UserId.Value,
                        activityId: assignment.ActivitySchedule.ActivityId,
                        points: movementPoints
                    );
                    Console.WriteLine($"[CreateEvaluation] Added {movementPoints} movement points for student {assignment.UserId.Value} from evaluation");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateEvaluation] Failed to add movement score: {ex.Message}");
                    // Don't fail the evaluation creation if movement score fails
                }
            }

            // Send notification to the evaluated user (if they have a UserId)
            if (assignment.UserId.HasValue)
            {
                var notification = new Notification
                {
                    Title = "Bạn có đánh giá mới",
                    Message = $"Bạn đã nhận được đánh giá cho vai trò {assignment.Role} trong sự kiện {assignment.ActivitySchedule.Activity.Title}",
                    Scope = "User",
                    TargetUserId = assignment.UserId.Value,
                    CreatedById = evaluatorId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationService.CreateAsync(notification);
            }

            // Check if all assignments of the activity have been evaluated
            await CheckAndNotifyManagerIfAllEvaluatedAsync(assignment.ActivitySchedule.ActivityId, evaluatorId);

            // Return DTO
            return await MapToDto(created);
        }

        public async Task<ActivityMemberEvaluationDto?> UpdateEvaluationAsync(int evaluatorId, int evaluationId, CreateActivityMemberEvaluationDto dto)
        {
            // Get existing evaluation
            var evaluation = await _evaluationRepository.GetByIdAsync(evaluationId);
            if (evaluation == null)
            {
                return null;
            }

            // Check if evaluator is the one who created it
            if (evaluation.EvaluatorId != evaluatorId)
            {
                // Check if evaluator is admin
                var evaluator = await _userRepository.GetByIdWithRolesAsync(evaluatorId);
                if (evaluator == null || evaluator.Role.RoleName != "Admin")
                {
                    throw new UnauthorizedAccessException("You can only update your own evaluations");
                }
            }

            // Validate scores
            ValidateScores(dto);

            // Validate text lengths
            ValidateTextLengths(dto);

            // Calculate average score
            var averageScore = (dto.ResponsibilityScore + dto.SkillScore + dto.AttitudeScore + dto.EffectivenessScore) / 4.0;

            // Update evaluation
            evaluation.ResponsibilityScore = dto.ResponsibilityScore;
            evaluation.SkillScore = dto.SkillScore;
            evaluation.AttitudeScore = dto.AttitudeScore;
            evaluation.EffectivenessScore = dto.EffectivenessScore;
            evaluation.AverageScore = averageScore;
            evaluation.Comments = dto.Comments;
            evaluation.Strengths = dto.Strengths;
            evaluation.Improvements = dto.Improvements;
            evaluation.UpdatedAt = DateTime.UtcNow;

            var updated = await _evaluationRepository.UpdateAsync(evaluation);

            // Update movement score if user exists
            var assignment = await _context.ActivityScheduleAssignments
                .Include(a => a.ActivitySchedule)
                .FirstOrDefaultAsync(a => a.Id == updated.ActivityScheduleAssignmentId);
            
            if (assignment?.UserId.HasValue == true)
            {
                try
                {
                    var movementPoints = Math.Round(updated.AverageScore, 1);
                    await _movementRecordService.UpdateScoreFromEvaluationAsync(
                        studentId: assignment.UserId.Value,
                        activityId: assignment.ActivitySchedule.ActivityId,
                        newPoints: movementPoints
                    );
                    Console.WriteLine($"[UpdateEvaluation] Updated movement points to {movementPoints} for student {assignment.UserId.Value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UpdateEvaluation] Failed to update movement score: {ex.Message}");
                    // Don't fail the evaluation update if movement score update fails
                }
            }

            // Return DTO
            return await MapToDto(updated);
        }

        private void ValidateScores(CreateActivityMemberEvaluationDto dto)
        {
            if (dto.ResponsibilityScore < 1 || dto.ResponsibilityScore > 10)
                throw new ArgumentException("ResponsibilityScore must be between 1 and 10");
            if (dto.SkillScore < 1 || dto.SkillScore > 10)
                throw new ArgumentException("SkillScore must be between 1 and 10");
            if (dto.AttitudeScore < 1 || dto.AttitudeScore > 10)
                throw new ArgumentException("AttitudeScore must be between 1 and 10");
            if (dto.EffectivenessScore < 1 || dto.EffectivenessScore > 10)
                throw new ArgumentException("EffectivenessScore must be between 1 and 10");
        }

        private void ValidateTextLengths(CreateActivityMemberEvaluationDto dto)
        {
            if (dto.Comments != null && dto.Comments.Length > 2000)
                throw new ArgumentException("Comments cannot exceed 2000 characters");
            if (dto.Strengths != null && dto.Strengths.Length > 1000)
                throw new ArgumentException("Strengths cannot exceed 1000 characters");
            if (dto.Improvements != null && dto.Improvements.Length > 1000)
                throw new ArgumentException("Improvements cannot exceed 1000 characters");
        }

        private async Task<ActivityMemberEvaluationDto> MapToDto(ActivityMemberEvaluation evaluation)
        {
            // Load related entities if not loaded
            if (evaluation.Assignment == null)
            {
                evaluation = await _evaluationRepository.GetByIdAsync(evaluation.Id) ?? evaluation;
            }

            var evaluator = await _userRepository.GetByIdAsync(evaluation.EvaluatorId);

            return new ActivityMemberEvaluationDto
            {
                Id = evaluation.Id,
                ActivityScheduleAssignmentId = evaluation.ActivityScheduleAssignmentId,
                EvaluatorId = evaluation.EvaluatorId,
                EvaluatorName = evaluator?.FullName ?? "Unknown",
                UserId = evaluation.Assignment.UserId,
                UserName = evaluation.Assignment.User?.FullName,
                ResponsibleName = evaluation.Assignment.ResponsibleName,
                Role = evaluation.Assignment.Role,
                ActivityId = evaluation.Assignment.ActivitySchedule.ActivityId,
                ActivityTitle = evaluation.Assignment.ActivitySchedule.Activity.Title,
                ScheduleTitle = evaluation.Assignment.ActivitySchedule.Title,
                StartTime = evaluation.Assignment.ActivitySchedule.StartTime,
                EndTime = evaluation.Assignment.ActivitySchedule.EndTime,
                ResponsibilityScore = evaluation.ResponsibilityScore,
                SkillScore = evaluation.SkillScore,
                AttitudeScore = evaluation.AttitudeScore,
                EffectivenessScore = evaluation.EffectivenessScore,
                AverageScore = evaluation.AverageScore,
                Comments = evaluation.Comments,
                Strengths = evaluation.Strengths,
                Improvements = evaluation.Improvements,
                CreatedAt = evaluation.CreatedAt,
                UpdatedAt = evaluation.UpdatedAt
            };
        }

        public async Task<ActivityMemberEvaluationDto?> GetEvaluationByIdAsync(int evaluationId)
        {
            var evaluation = await _evaluationRepository.GetByIdAsync(evaluationId);
            if (evaluation == null)
            {
                return null;
            }

            return await MapToDto(evaluation);
        }

        public async Task<ActivityMemberEvaluationDto?> GetEvaluationByAssignmentIdAsync(int assignmentId)
        {
            var evaluation = await _evaluationRepository.GetByAssignmentIdAsync(assignmentId);
            if (evaluation == null)
            {
                return null;
            }

            return await MapToDto(evaluation);
        }

        public async Task<List<ActivityMemberEvaluationDto>> GetMyEvaluationsAsync(int userId)
        {
            // Get all evaluations where the user was evaluated
            var evaluations = await _evaluationRepository.GetByUserIdAsync(userId);

            var result = new List<ActivityMemberEvaluationDto>();
            foreach (var evaluation in evaluations)
            {
                result.Add(await MapToDto(evaluation));
            }

            return result;
        }

        public async Task<ActivityEvaluationReportDto> GetActivityEvaluationReportAsync(int activityId)
        {
            // Get activity
            var activity = await _activityRepository.GetByIdAsync(activityId);
            if (activity == null)
            {
                throw new ArgumentException("Activity not found");
            }

            // Get all assignments for this activity
            var assignments = await _context.ActivityScheduleAssignments
                .Include(a => a.ActivitySchedule)
                .Include(a => a.User)
                .Where(a => a.ActivitySchedule.ActivityId == activityId)
                .ToListAsync();

            // Get all evaluations for this activity
            var evaluations = await _evaluationRepository.GetByActivityIdAsync(activityId);
            var evaluationDict = evaluations.ToDictionary(e => e.ActivityScheduleAssignmentId);

            // Build member list
            var members = assignments.Select(a =>
            {
                var hasEvaluation = evaluationDict.TryGetValue(a.Id, out var evaluation);
                return new ActivityMemberEvaluationListDto
                {
                    AssignmentId = a.Id,
                    UserId = a.UserId,
                    UserName = a.User?.FullName,
                    ResponsibleName = a.ResponsibleName,
                    Role = a.Role,
                    ScheduleTitle = a.ActivitySchedule.Title,
                    StartTime = a.ActivitySchedule.StartTime,
                    EndTime = a.ActivitySchedule.EndTime,
                    IsEvaluated = hasEvaluation,
                    AverageScore = hasEvaluation ? evaluation!.AverageScore : null,
                    EvaluatedAt = hasEvaluation ? evaluation!.CreatedAt : null
                };
            }).ToList();

            // Calculate statistics
            var report = new ActivityEvaluationReportDto
            {
                ActivityId = activityId,
                ActivityTitle = activity.Title,
                TotalAssignments = assignments.Count,
                EvaluatedCount = evaluations.Count,
                UnevaluatedCount = assignments.Count - evaluations.Count,
                Members = members.Cast<AssignmentEvaluationListDto>().ToList()
            };

            // Calculate average scores if there are evaluations
            if (evaluations.Any())
            {
                report.OverallAverageScore = evaluations.Average(e => e.AverageScore);
                report.AvgResponsibilityScore = evaluations.Average(e => e.ResponsibilityScore);
                report.AvgSkillScore = evaluations.Average(e => e.SkillScore);
                report.AvgAttitudeScore = evaluations.Average(e => e.AttitudeScore);
                report.AvgEffectivenessScore = evaluations.Average(e => e.EffectivenessScore);
            }

            return report;
        }

        public async Task<MemberEvaluationHistoryDto> GetMemberEvaluationHistoryAsync(int userId)
        {
            // Get user
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Get all evaluations for this user
            var evaluations = await _evaluationRepository.GetByUserIdAsync(userId);

            // Map to DTOs and sort by date (newest first)
            var evaluationDtos = new List<ActivityMemberEvaluationDto>();
            foreach (var evaluation in evaluations.OrderByDescending(e => e.CreatedAt))
            {
                evaluationDtos.Add(await MapToDto(evaluation));
            }

            // Calculate overall average
            double? overallAverage = null;
            if (evaluations.Any())
            {
                overallAverage = evaluations.Average(e => e.AverageScore);
            }

            return new MemberEvaluationHistoryDto
            {
                UserId = userId,
                UserName = user.FullName,
                TotalEvaluations = evaluations.Count,
                OverallAverageScore = overallAverage,
                Evaluations = evaluationDtos.Cast<AssignmentEvaluationDto>().ToList()
            };
        }

        public async Task<bool> CanEvaluateAsync(int evaluatorId, int activityId)
        {
            // Get user with roles
            var user = await _userRepository.GetByIdWithRolesAsync(evaluatorId);
            if (user == null)
            {
                return false;
            }

            // Admin can evaluate any activity
            if (user.Role.RoleName == "Admin")
            {
                return true;
            }

            // Get activity
            var activity = await _activityRepository.GetByIdAsync(activityId);
            if (activity == null)
            {
                return false;
            }

            // If activity has a club, check if user is manager of that club
            if (activity.ClubId.HasValue)
            {
                var clubMember = await _clubMemberRepository.GetByClubAndUserIdAsync(activity.ClubId.Value, evaluatorId);
                if (clubMember != null && clubMember.RoleInClub == "Manager" && clubMember.IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task CheckAndNotifyManagerIfAllEvaluatedAsync(int activityId, int evaluatorId)
        {
            // Get all assignments for this activity
            var assignments = await _context.ActivityScheduleAssignments
                .Include(a => a.ActivitySchedule)
                .Where(a => a.ActivitySchedule.ActivityId == activityId)
                .ToListAsync();

            if (assignments.Count == 0)
            {
                return;
            }

            // Get all evaluations for this activity
            var evaluations = await _evaluationRepository.GetByActivityIdAsync(activityId);

            // Check if all assignments have been evaluated
            if (evaluations.Count == assignments.Count)
            {
                // Get activity with club info
                var activity = await _context.Activities
                    .Include(a => a.Club)
                    .FirstOrDefaultAsync(a => a.Id == activityId);

                if (activity == null || !activity.ClubId.HasValue)
                {
                    return;
                }

                // Get all managers of the club
                var managers = await _context.ClubMembers
                    .Include(cm => cm.Student)
                    .Where(cm => cm.ClubId == activity.ClubId.Value 
                        && cm.RoleInClub == "Manager" 
                        && cm.IsActive)
                    .ToListAsync();

                // Send notification to each manager
                foreach (var manager in managers)
                {
                    var notification = new Notification
                    {
                        Title = "Hoàn thành đánh giá sự kiện",
                        Message = $"Tất cả thành viên trong sự kiện {activity.Title} đã được đánh giá",
                        Scope = "User",
                        TargetUserId = manager.Student.UserId,
                        CreatedById = evaluatorId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationService.CreateAsync(notification);
                }
            }
        }
    }
}
