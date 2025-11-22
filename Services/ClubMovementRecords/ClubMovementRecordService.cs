using BusinessObject.Models;
using BusinessObject.Enum;
using Repositories.ClubMovementRecords;
using Repositories.MovementCriteria;
using Repositories.Semesters;
using Microsoft.Extensions.Logging;

namespace Services.ClubMovementRecords;

public class ClubMovementRecordService : IClubMovementRecordService
{
    private readonly IClubMovementRecordRepository _recordRepo;
    private readonly IClubMovementRecordDetailRepository _detailRepo;
    private readonly IMovementCriterionRepository _criterionRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly ILogger<ClubMovementRecordService> _logger;

    public ClubMovementRecordService(
        IClubMovementRecordRepository recordRepo,
        IClubMovementRecordDetailRepository detailRepo,
        IMovementCriterionRepository criterionRepo,
        ISemesterRepository semesterRepo,
        ILogger<ClubMovementRecordService> logger)
    {
        _recordRepo = recordRepo;
        _detailRepo = detailRepo;
        _criterionRepo = criterionRepo;
        _semesterRepo = semesterRepo;
        _logger = logger;
    }

    public async Task<(double organizingPoints, double? collaboratingPoints)> AwardActivityPointsAsync(Activity activity)
    {
        double organizingPoints = 0;
        double? collaboratingPoints = null;

        try
        {
            _logger.LogInformation("[AWARD POINTS] Starting point calculation for Activity {ActivityId}, Type={Type}, ClubId={ClubId}, CollaborationClubId={CollaborationClubId}",
                activity.Id, activity.Type, activity.ClubId, activity.ClubCollaborationId);

            // Award points to organizing club if ClubId exists (Requirement 9.1)
            if (activity.ClubId.HasValue)
            {
                try
                {
                    organizingPoints = await AwardPointsToClubAsync(
                        activity.ClubId.Value,
                        activity,
                        activity.MovementPoint,
                        false);
                    
                    _logger.LogInformation("[AWARD POINTS] Organizing club {ClubId} awarded {Points} points for Activity {ActivityId}",
                        activity.ClubId.Value, organizingPoints, activity.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AWARD POINTS] Failed to award points to organizing club {ClubId} for Activity {ActivityId}. Error: {ErrorMessage}",
                        activity.ClubId.Value, activity.Id, ex.Message);
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("[AWARD POINTS] Activity {ActivityId} has no organizing club. Skipping organizing club points.", activity.Id);
            }

            // Award collaboration points if ClubCollaborationId exists
            if (activity.ClubCollaborationId.HasValue && activity.CollaborationPoint.HasValue)
            {
                try
                {
                    collaboratingPoints = await AwardPointsToClubAsync(
                        activity.ClubCollaborationId.Value,
                        activity,
                        activity.CollaborationPoint.Value,
                        true);
                    
                    _logger.LogInformation("[AWARD POINTS] Collaborating club {ClubId} awarded {Points} points for Activity {ActivityId}",
                        activity.ClubCollaborationId.Value, collaboratingPoints, activity.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AWARD POINTS] Failed to award collaboration points to club {ClubId} for Activity {ActivityId}. Error: {ErrorMessage}",
                        activity.ClubCollaborationId.Value, activity.Id, ex.Message);
                    throw;
                }
            }

            _logger.LogInformation("[AWARD POINTS] Completed point calculation for Activity {ActivityId}. Organizing={OrganizingPoints}, Collaborating={CollaboratingPoints}",
                activity.Id, organizingPoints, collaboratingPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AWARD POINTS] Error awarding points for Activity {ActivityId}, ClubId={ClubId}, CollaborationClubId={CollaborationClubId}. Error: {ErrorMessage}",
                activity.Id, activity.ClubId, activity.ClubCollaborationId, ex.Message);
            throw;
        }

        return (organizingPoints, collaboratingPoints);
    }

    private async Task<double> AwardPointsToClubAsync(
        int clubId,
        Activity activity,
        double points,
        bool isCollaboration)
    {
        try
        {
            _logger.LogInformation("[AWARD TO CLUB] Starting point award for Club {ClubId}, Activity {ActivityId}, Points={Points}, IsCollaboration={IsCollaboration}",
                clubId, activity.Id, points, isCollaboration);

            // Get the current semester
            var semester = await _semesterRepo.GetCurrentSemesterAsync();
            if (semester == null)
            {
                _logger.LogWarning("[AWARD TO CLUB] No active semester found. Cannot award points for Activity {ActivityId}, ClubId={ClubId}",
                    activity.Id, clubId);
                return 0;
            }

            _logger.LogInformation("[AWARD TO CLUB] Using semester {SemesterId} for Club {ClubId}, Activity {ActivityId}",
                semester.Id, clubId, activity.Id);

            // Get MovementCriterion for this activity type (Requirement 9.2)
            var criterion = await GetCriterionForActivityTypeAsync(activity.Type);
            if (criterion == null)
            {
                _logger.LogError("[AWARD TO CLUB] No criterion found for activity type {ActivityType}. Activity {ActivityId}, ClubId={ClubId} completed without points.",
                    activity.Type, activity.Id, clubId);
                return 0;
            }

            _logger.LogInformation("[AWARD TO CLUB] Using criterion {CriterionId} ({CriterionTitle}) for Activity {ActivityId}, ClubId={ClubId}",
                criterion.Id, criterion.Title, activity.Id, clubId);

            // Determine score category
            var scoreCategory = GetScoreCategory(activity.Type);

            // Check weekly limit for Club Activities
            double actualPoints = points;
            string? note = null;

            if (IsClubActivity(activity.Type) && !isCollaboration)
            {
                try
                {
                    var weeklyPoints = await GetWeeklyClubActivityPointsAsync(clubId, semester.Id, activity.EndTime);
                    _logger.LogInformation("[AWARD TO CLUB] Weekly club activity points for Club {ClubId}: {WeeklyPoints}/5",
                        clubId, weeklyPoints);

                    if (weeklyPoints >= 5)
                    {
                        actualPoints = 0;
                        note = $"Weekly limit reached (5 points). Activity: {activity.Title}";
                        _logger.LogInformation("[AWARD TO CLUB] Weekly limit reached for Club {ClubId}. No points awarded for Activity {ActivityId}",
                            clubId, activity.Id);
                    }
                    else if (weeklyPoints + points > 5)
                    {
                        actualPoints = 5 - weeklyPoints;
                        note = $"Partial points awarded due to weekly limit. Activity: {activity.Title}";
                        _logger.LogInformation("[AWARD TO CLUB] Partial points awarded to Club {ClubId}: {ActualPoints} (weekly limit)",
                            clubId, actualPoints);
                    }
                    else
                    {
                        note = $"Club Activity: {activity.Title}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AWARD TO CLUB] Error calculating weekly points for Club {ClubId}, Activity {ActivityId}. Error: {ErrorMessage}",
                        clubId, activity.Id, ex.Message);
                    throw;
                }
            }
            else if (isCollaboration)
            {
                note = $"Collaboration with {activity.Club?.Name ?? "organizing club"}. Activity: {activity.Title}";
            }
            else
            {
                note = $"{activity.Type}: {activity.Title}";
            }

            // Get or create ClubMovementRecord for the month
            var month = activity.EndTime.Month;
            ClubMovementRecord record;
            
            try
            {
                record = await _recordRepo.GetByClubMonthAsync(clubId, semester.Id, month);

                if (record == null)
                {
                    _logger.LogInformation("[AWARD TO CLUB] Creating new ClubMovementRecord for Club {ClubId}, Semester {SemesterId}, Month {Month}",
                        clubId, semester.Id, month);

                    record = new ClubMovementRecord
                    {
                        ClubId = clubId,
                        SemesterId = semester.Id,
                        Month = month,
                        ClubMeetingScore = 0,
                        EventScore = 0,
                        CompetitionScore = 0,
                        PlanScore = 0,
                        CollaborationScore = 0,
                        TotalScore = 0
                    };
                    record = await _recordRepo.CreateAsync(record);
                    
                    _logger.LogInformation("[AWARD TO CLUB] Created ClubMovementRecord {RecordId} for Club {ClubId}",
                        record.Id, clubId);
                }
                else
                {
                    _logger.LogInformation("[AWARD TO CLUB] Using existing ClubMovementRecord {RecordId} for Club {ClubId}",
                        record.Id, clubId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AWARD TO CLUB] Error getting/creating ClubMovementRecord for Club {ClubId}, Semester {SemesterId}, Month {Month}. Error: {ErrorMessage}",
                    clubId, semester.Id, month, ex.Message);
                throw;
            }

            // Create ClubMovementRecordDetail first (Single Source of Truth)
            try
            {
                var detail = new ClubMovementRecordDetail
                {
                    ClubMovementRecordId = record.Id,
                    CriterionId = criterion.Id,
                    ActivityId = activity.Id,
                    Score = actualPoints,
                    ScoreType = "Manual",  // Manual completion by ClubManager - won't be deleted by auto-scoring
                    Note = note,
                    CreatedBy = null // Auto-scoring has no user
                };

                await _detailRepo.CreateAsync(detail);
                
                _logger.LogInformation("[AWARD TO CLUB] Created ClubMovementRecordDetail for Club {ClubId}, Activity {ActivityId}, Score={Score}, Category={Category}, ScoreType=Manual",
                    clubId, activity.Id, actualPoints, scoreCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AWARD TO CLUB] Error creating ClubMovementRecordDetail for Club {ClubId}, Activity {ActivityId}. Error: {ErrorMessage}",
                    clubId, activity.Id, ex.Message);
                throw;
            }

            // Recalculate scores from Details (Single Source of Truth)
            try
            {
                var previousTotal = record.TotalScore;
                await _recordRepo.RecalculateTotalScoreAsync(record.Id);
                
                // Reload record to get updated scores
                record = await _recordRepo.GetByIdAsync(record.Id);
                
                _logger.LogInformation("[AWARD TO CLUB] Recalculated scores for ClubMovementRecord {RecordId}. Previous total: {PreviousTotal}, New total: {NewTotal}",
                    record.Id, previousTotal, record?.TotalScore ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AWARD TO CLUB] Error recalculating scores for ClubMovementRecord {RecordId}, Club {ClubId}. Error: {ErrorMessage}",
                    record.Id, clubId, ex.Message);
                throw;
            }

            _logger.LogInformation("[AWARD TO CLUB] Successfully awarded {Points} points to Club {ClubId} for Activity {ActivityId} in category {Category}",
                actualPoints, clubId, activity.Id, scoreCategory);

            return actualPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AWARD TO CLUB] Unexpected error awarding points to Club {ClubId} for Activity {ActivityId}. Error: {ErrorMessage}",
                clubId, activity.Id, ex.Message);
            throw;
        }
    }

    // Note: UpdateScoreCategory method is no longer used as we now calculate scores from Details (Single Source of Truth)

    private async Task<double> GetWeeklyClubActivityPointsAsync(int clubId, int semesterId, DateTime activityEndTime)
    {
        // Calculate week boundaries (Monday to Sunday)
        var dayOfWeek = (int)activityEndTime.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday = 0, Monday = 1
        var weekStart = activityEndTime.Date.AddDays(-daysFromMonday);
        var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

        _logger.LogInformation("[WEEKLY LIMIT] Checking week {WeekStart} to {WeekEnd} for Club {ClubId}",
            weekStart, weekEnd, clubId);

        // Get all details for this club in the current week
        var allRecords = await _recordRepo.GetByClubAsync(clubId, semesterId);
        var allDetails = allRecords.SelectMany(r => r.Details).ToList();
        
        _logger.LogInformation("[WEEKLY LIMIT] Found {TotalDetails} total details for Club {ClubId}",
            allDetails.Count, clubId);

        var weeklyDetails = allDetails
            .Where(d => d.Activity != null &&
                       d.Activity.EndTime >= weekStart &&
                       d.Activity.EndTime <= weekEnd &&
                       IsClubActivity(d.Activity.Type))
            .ToList();

        _logger.LogInformation("[WEEKLY LIMIT] Found {WeeklyDetails} club activity details in current week for Club {ClubId}. Activities: {ActivityIds}",
            weeklyDetails.Count, clubId, string.Join(", ", weeklyDetails.Select(d => $"{d.ActivityId}({d.Score}pts)")));

        return weeklyDetails.Sum(d => d.Score);
    }

    private async Task<MovementCriterion?> GetCriterionForActivityTypeAsync(ActivityType activityType)
    {
        try
        {
            _logger.LogInformation("[GET CRITERION] Looking up criterion for activity type {ActivityType}", activityType);

            var allCriteria = await _criterionRepo.GetByTargetTypeAsync("Club");
            var activeCriteria = allCriteria.Where(c => c.IsActive).ToList();

            _logger.LogInformation("[GET CRITERION] Found {Count} active criteria for target type 'Club'", activeCriteria.Count);

            // Map activity type to criterion title patterns
            var searchPattern = activityType switch
            {
                ActivityType.ClubMeeting or ActivityType.ClubTraining or ActivityType.ClubWorkshop
                    => "Sinh hoạt CLB",
                ActivityType.LargeEvent or ActivityType.MediumEvent or ActivityType.SmallEvent
                    => "Sự kiện",
                ActivityType.SchoolCompetition or ActivityType.ProvincialCompetition or ActivityType.NationalCompetition
                    => "thi",
                ActivityType.ClubCollaboration or ActivityType.SchoolCollaboration
                    => "Phối hợp",
                _ => null
            };

            if (searchPattern == null)
            {
                _logger.LogWarning("[GET CRITERION] No search pattern defined for activity type {ActivityType}", activityType);
                return null;
            }

            _logger.LogInformation("[GET CRITERION] Searching for criterion with pattern '{SearchPattern}' for activity type {ActivityType}",
                searchPattern, activityType);

            // Find matching criterion (Requirement 9.2)
            var criterion = activeCriteria.FirstOrDefault(c =>
                c.Title.Contains(searchPattern, StringComparison.OrdinalIgnoreCase));

            if (criterion == null)
            {
                _logger.LogError("[GET CRITERION] No criterion found matching pattern '{SearchPattern}' for activity type {ActivityType}. Available criteria: {AvailableCriteria}",
                    searchPattern, activityType, string.Join(", ", activeCriteria.Select(c => c.Title)));
            }
            else
            {
                _logger.LogInformation("[GET CRITERION] Found criterion {CriterionId} ({CriterionTitle}) for activity type {ActivityType}",
                    criterion.Id, criterion.Title, activityType);
            }

            return criterion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GET CRITERION] Error retrieving criterion for activity type {ActivityType}. Error: {ErrorMessage}",
                activityType, ex.Message);
            throw;
        }
    }

    private string GetScoreCategory(ActivityType activityType)
    {
        return activityType switch
        {
            ActivityType.ClubMeeting or ActivityType.ClubTraining or ActivityType.ClubWorkshop
                => "ClubMeetingScore",
            ActivityType.LargeEvent or ActivityType.MediumEvent or ActivityType.SmallEvent
                => "EventScore",
            ActivityType.SchoolCompetition or ActivityType.ProvincialCompetition or ActivityType.NationalCompetition
                => "CompetitionScore",
            ActivityType.ClubCollaboration or ActivityType.SchoolCollaboration
                => "CollaborationScore",
            _ => "EventScore" // Default fallback
        };
    }

    private bool IsClubActivity(ActivityType activityType)
    {
        return activityType == ActivityType.ClubMeeting ||
               activityType == ActivityType.ClubTraining ||
               activityType == ActivityType.ClubWorkshop;
    }

    // DEPRECATED: This method is no longer used. Scores are now calculated from Details (Single Source of Truth)
    // via RecalculateTotalScoreAsync() to ensure consistency and avoid race conditions.
    // Keeping this for reference only - can be safely removed in future cleanup.
    /*
    private void UpdateScoreCategory(ClubMovementRecord record, string category, double points)
    {
        switch (category)
        {
            case "ClubMeetingScore":
                record.ClubMeetingScore += points;
                break;
            case "EventScore":
                record.EventScore += points;
                break;
            case "CompetitionScore":
                record.CompetitionScore += points;
                break;
            case "CollaborationScore":
                record.CollaborationScore += points;
                break;
        }
    }
    */
}
