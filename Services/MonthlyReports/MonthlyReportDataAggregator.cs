using BusinessObject.DTOs.MonthlyReport;
using BusinessObject.Enum;
using Repositories.Activities;
using Repositories.ActivityEvaluations;
using Repositories.ActivityMemberEvaluations;
using Repositories.CommunicationPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataAccess;
using System.Text.Json;

namespace Services.MonthlyReports;

public class MonthlyReportDataAggregator : IMonthlyReportDataAggregator
{
    private readonly IActivityRepository _activityRepo;
    private readonly IActivityMemberEvaluationRepository _memberEvalRepo;
    private readonly ICommunicationPlanRepository _communicationPlanRepo;
    private readonly EduXtendContext _context;
    private readonly ILogger<MonthlyReportDataAggregator> _logger;

    public MonthlyReportDataAggregator(
        IActivityRepository activityRepo,
        IActivityMemberEvaluationRepository memberEvalRepo,
        ICommunicationPlanRepository communicationPlanRepo,
        EduXtendContext context,
        ILogger<MonthlyReportDataAggregator> logger)
    {
        _activityRepo = activityRepo;
        _memberEvalRepo = memberEvalRepo;
        _communicationPlanRepo = communicationPlanRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<List<SchoolEventDto>> GetSchoolEventsAsync(int clubId, int month, int year)
    {
        // Get activities that are School Events (Large, Medium, Small Events)
        var activities = await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId
                && a.StartTime.Month == month
                && a.StartTime.Year == year
                && (a.Type == ActivityType.LargeEvent 
                    || a.Type == ActivityType.MediumEvent 
                    || a.Type == ActivityType.SmallEvent))
            .Include(a => a.Attendances)
                .ThenInclude(att => att.User)
                    .ThenInclude(u => u.Role)
            .Include(a => a.Evaluation)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        var schoolEvents = new List<SchoolEventDto>();

        foreach (var activity in activities)
        {
            // Get participants (students who attended)
            var participants = activity.Attendances
                .Where(att => att.IsPresent)
                .Select(att => new ParticipantDto
                {
                    FullName = att.User.FullName,
                    StudentCode = GetStudentCode(att.User.Id),
                    PhoneNumber = att.User.PhoneNumber ?? "",
                    Rating = att.ParticipationScore
                })
                .ToList();

            // Get support members (club members who helped organize)
            var supportMembers = await GetSupportMembersAsync(activity.Id, clubId);

            // Map evaluation data
            EventEvaluationDto? evaluation = null;
            if (activity.Evaluation != null)
            {
                var eval = activity.Evaluation;
                evaluation = new EventEvaluationDto
                {
                    ExpectedCount = eval.ExpectedParticipants,
                    ActualCount = eval.ActualParticipants,
                    ReasonIfLess = eval.Reason,
                    CommunicationScore = eval.CommunicationScore,
                    OrganizationScore = eval.OrganizationScore,
                    McHostEvaluation = $"Score: {eval.HostScore}/10",
                    SpeakerPerformerEvaluation = $"Score: {eval.SpeakerScore}/10",
                    Achievements = $"Success Score: {eval.Success}/10",
                    Limitations = eval.Limitations,
                    ProposedSolutions = eval.ImprovementMeasures
                };
            }

            // Get timeline from ActivitySchedules
            var timeline = await GetActivityTimelineAsync(activity.Id);

            var schoolEvent = new SchoolEventDto
            {
                ActivityId = activity.Id,
                EventDate = activity.StartTime,
                EventName = activity.Title,
                ActualParticipants = participants.Count,
                Participants = participants,
                Evaluation = evaluation ?? new EventEvaluationDto(),
                SupportMembers = supportMembers,
                Timeline = timeline,
                FeedbackUrl = null, // TODO: Get from activity if available
                MediaUrls = activity.ImageUrl
            };

            schoolEvents.Add(schoolEvent);
        }

        return schoolEvents;
    }

    public async Task<List<SupportActivityDto>> GetSupportActivitiesAsync(int clubId, int month, int year)
    {
        // Get activities that are Support activities (SchoolCollaboration)
        var activities = await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId
                && a.StartTime.Month == month
                && a.StartTime.Year == year
                && a.Type == ActivityType.SchoolCollaboration)
            .Include(a => a.Attendances)
                .ThenInclude(att => att.User)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        var supportActivities = new List<SupportActivityDto>();

        foreach (var activity in activities)
        {
            var supportStudents = activity.Attendances
                .Where(att => att.IsPresent)
                .Select(att => new SupportStudentDto
                {
                    FullName = att.User.FullName,
                    StudentCode = GetStudentCode(att.User.Id),
                    EventName = activity.Title,
                    EventTime = activity.StartTime,
                    Rating = att.ParticipationScore
                })
                .ToList();

            var supportActivity = new SupportActivityDto
            {
                ActivityId = activity.Id,
                EventContent = activity.Title,
                DepartmentName = activity.Description ?? "", // Department name stored in description
                EventTime = activity.StartTime,
                Location = activity.Location ?? "",
                ImageUrl = activity.ImageUrl,
                SupportStudents = supportStudents
            };

            supportActivities.Add(supportActivity);
        }

        return supportActivities;
    }

    public async Task<List<CompetitionDto>> GetCompetitionsAsync(int clubId, int month, int year)
    {
        // Get activities that are Competitions
        var activities = await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId
                && a.StartTime.Month == month
                && a.StartTime.Year == year
                && (a.Type == ActivityType.SchoolCompetition
                    || a.Type == ActivityType.ProvincialCompetition
                    || a.Type == ActivityType.NationalCompetition))
            .Include(a => a.Attendances)
                .ThenInclude(att => att.User)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        var competitions = new List<CompetitionDto>();

        foreach (var activity in activities)
        {
            var participants = activity.Attendances
                .Where(att => att.IsPresent)
                .Select(att => new CompetitionParticipantDto
                {
                    FullName = att.User.FullName,
                    StudentCode = GetStudentCode(att.User.Id),
                    Email = att.User.Email,
                    Achievement = null, // TODO: Store achievement data in a custom field
                    Note = null
                })
                .ToList();

            var competition = new CompetitionDto
            {
                ActivityId = activity.Id,
                CompetitionName = activity.Title,
                OrganizingUnit = activity.Description ?? "", // Organizing unit stored in description
                Participants = participants
            };

            competitions.Add(competition);
        }

        return competitions;
    }

    public async Task<List<InternalMeetingDto>> GetInternalMeetingsAsync(int clubId, int month, int year)
    {
        // Get activities that are Internal meetings
        var activities = await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId
                && a.StartTime.Month == month
                && a.StartTime.Year == year
                && (a.Type == ActivityType.ClubMeeting
                    || a.Type == ActivityType.ClubTraining
                    || a.Type == ActivityType.ClubWorkshop))
            .Include(a => a.Attendances)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        var internalMeetings = activities.Select(activity => new InternalMeetingDto
        {
            ActivityId = activity.Id,
            MeetingTime = activity.StartTime,
            Location = activity.Location ?? "",
            ParticipantCount = activity.Attendances.Count(att => att.IsPresent),
            Content = activity.Description ?? "",
            ImageUrl = activity.ImageUrl
        }).ToList();

        return internalMeetings;
    }

    public async Task<NextMonthPlansDto> GetNextMonthPlansAsync(int clubId, int reportMonth, int reportYear, int nextMonth, int nextYear)
    {
        var result = new NextMonthPlansDto
        {
            Purpose = new PurposeDto(),
            PlannedEvents = new List<PlannedEventDto>(),
            PlannedCompetitions = new List<PlannedCompetitionDto>(),
            CommunicationPlan = new List<CommunicationItemDto>(),
            Budget = new BudgetDto
            {
                SchoolFunding = new List<BudgetItemDto>(),
                ClubFunding = new List<BudgetItemDto>()
            },
            Facility = new FacilityDto
            {
                Items = new List<FacilityItemDto>()
            },
            Responsibilities = new ClubResponsibilitiesDto()
        };

        // Get the Plan record for the REPORT month (which contains editable sections)
        var plan = await _context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClubId == clubId 
                && p.ReportType == "Monthly"
                && p.ReportMonth == reportMonth 
                && p.ReportYear == reportYear);

        // Load Purpose and Significance from Plan (editable section)
        if (plan != null && !string.IsNullOrEmpty(plan.NextMonthPurposeAndSignificance))
        {
            try
            {
                var purposeData = JsonSerializer.Deserialize<PurposeDto>(plan.NextMonthPurposeAndSignificance);
                if (purposeData != null)
                {
                    result.Purpose = purposeData;
                }
            }
            catch
            {
                // If JSON parsing fails, leave as empty
            }
        }

        // Load Club Responsibilities from Plan (editable section)
        if (plan != null && !string.IsNullOrEmpty(plan.ClubResponsibilities))
        {
            try
            {
                _logger.LogInformation("Raw ClubResponsibilities JSON: {Json}", plan.ClubResponsibilities);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var responsibilitiesData = JsonSerializer.Deserialize<ClubResponsibilitiesDto>(plan.ClubResponsibilities, options);
                if (responsibilitiesData != null)
                {
                    _logger.LogInformation("Deserialized CustomText: {CustomText}", responsibilitiesData.CustomText);
                    result.Responsibilities = responsibilitiesData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse ClubResponsibilities JSON");
                // If JSON parsing fails, leave as empty
            }
        }

        // Get planned events for next month (School Events)
        var plannedEvents = await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId
                && a.StartTime.Month == nextMonth
                && a.StartTime.Year == nextYear
                && (a.Type == ActivityType.LargeEvent 
                    || a.Type == ActivityType.MediumEvent 
                    || a.Type == ActivityType.SmallEvent))
            .Include(a => a.Registrations)
                .ThenInclude(r => r.User)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        result.PlannedEvents = plannedEvents.Select(activity => new PlannedEventDto
        {
            PlanId = activity.Id,
            EventName = activity.Title,
            EventContent = activity.Description ?? "",
            OrganizationTime = activity.StartTime,
            Location = activity.Location ?? "",
            ExpectedStudents = activity.MaxParticipants ?? 0,
            RegistrationUrl = null, // TODO: Add registration URL field to Activity if needed
            Timeline = "", // TODO: Add timeline field to Activity if needed
            Guests = new List<GuestDto>() // TODO: Add guest tracking if needed
        }).ToList();

        // Get planned competitions for next month
        var plannedCompetitions = await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId
                && a.StartTime.Month == nextMonth
                && a.StartTime.Year == nextYear
                && (a.Type == ActivityType.SchoolCompetition
                    || a.Type == ActivityType.ProvincialCompetition
                    || a.Type == ActivityType.NationalCompetition))
            .Include(a => a.Registrations)
                .ThenInclude(r => r.User)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        result.PlannedCompetitions = plannedCompetitions.Select(activity => new PlannedCompetitionDto
        {
            CompetitionName = activity.Title,
            AuthorizedUnit = activity.Description ?? "",
            CompetitionTime = activity.StartTime,
            Location = activity.Location ?? "",
            Participants = activity.Registrations.Select(r => new CompetitionParticipantDto
            {
                FullName = r.User.FullName,
                StudentCode = GetStudentCode(r.User.Id),
                Email = r.User.Email,
                Achievement = null,
                Note = null
            }).ToList()
        }).ToList();

        // Get communication plans for next month
        var communicationPlans = await _communicationPlanRepo.GetByClubAndMonthAsync(clubId, nextMonth, nextYear);

        foreach (var commPlan in communicationPlans)
        {
            foreach (var item in commPlan.Items)
            {
                result.CommunicationPlan.Add(new CommunicationItemDto
                {
                    Content = item.Content,
                    Time = item.ScheduledDate,
                    ResponsiblePerson = item.ResponsiblePerson ?? "",
                    NeedSupport = !string.IsNullOrEmpty(item.Notes) && item.Notes.Contains("support", StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        // Budget and Facility data are typically manually entered
        // These would need to be stored in the Plan model or separate tables
        // For now, they remain empty and can be populated from Plan.ReportSnapshot if needed

        return result;
    }

    // Helper methods

    private string GetStudentCode(int userId)
    {
        var student = _context.Students
            .AsNoTracking()
            .FirstOrDefault(s => s.UserId == userId);
        
        return student?.StudentCode ?? "";
    }

    private async Task<List<SupportMemberDto>> GetSupportMembersAsync(int activityId, int clubId)
    {
        // Get club members who helped organize the event
        // These are members with ActivityMemberEvaluation records
        var memberEvaluations = await _memberEvalRepo.GetByActivityIdAsync(activityId);

        var supportMembers = new List<SupportMemberDto>();

        foreach (var eval in memberEvaluations)
        {
            // Get user from assignment
            var assignment = await _context.ActivityScheduleAssignments
                .AsNoTracking()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == eval.ActivityScheduleAssignmentId);

            if (assignment?.User != null)
            {
                supportMembers.Add(new SupportMemberDto
                {
                    FullName = assignment.User.FullName,
                    StudentCode = GetStudentCode(assignment.User.Id),
                    PhoneNumber = assignment.User.PhoneNumber ?? "",
                    Position = assignment.Role ?? "",
                    Rating = (decimal)eval.AverageScore
                });
            }
            else if (assignment != null && !string.IsNullOrEmpty(assignment.ResponsibleName))
            {
                // External member
                supportMembers.Add(new SupportMemberDto
                {
                    FullName = assignment.ResponsibleName,
                    StudentCode = "",
                    PhoneNumber = "",
                    Position = assignment.Role ?? "",
                    Rating = (decimal)eval.AverageScore
                });
            }
        }

        return supportMembers;
    }

    private async Task<string> GetActivityTimelineAsync(int activityId)
    {
        // Get activity schedules
        var schedules = await _context.ActivitySchedules
            .AsNoTracking()
            .Where(s => s.ActivityId == activityId)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        if (!schedules.Any())
            return string.Empty;

        // Format timeline as text
        var timelineItems = schedules.Select(s => 
            $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}: {s.Title}"
        );

        return string.Join("\n", timelineItems);
    }
}

