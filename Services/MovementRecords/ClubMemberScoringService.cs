using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.MovementRecords;
using Repositories.Semesters;
using Repositories.MovementCriteria;

namespace Services.MovementRecords;

/// <summary>
/// Service for evaluating and scoring club members per Decision 414
/// Handles monthly evaluations and automatic score calculation
/// </summary>
public interface IClubMemberScoringService
{
    Task ProcessClubMembersAsync(int semesterId);
    Task<double> CalculateClubMemberScoreAsync(int studentId, int clubId, int monthYear);
    Task AddClubMembershipScoreAsync(int studentId, int clubId, int semesterId, double score, string notes);
}

public class ClubMemberScoringService : IClubMemberScoringService
{
    private readonly EduXtendContext _context;
    private readonly IMovementRecordRepository _recordRepository;
    private readonly IMovementRecordDetailRepository _detailRepository;
    private readonly IMovementCriterionRepository _criterionRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ILogger<ClubMemberScoringService> _logger;

    public ClubMemberScoringService(
        EduXtendContext context,
        IMovementRecordRepository recordRepository,
        IMovementRecordDetailRepository detailRepository,
        IMovementCriterionRepository criterionRepository,
        ISemesterRepository semesterRepository,
        ILogger<ClubMemberScoringService> logger)
    {
        _context = context;
        _recordRepository = recordRepository;
        _detailRepository = detailRepository;
        _criterionRepository = criterionRepository;
        _semesterRepository = semesterRepository;
        _logger = logger;
    }

    /// <summary>
    /// Process all club members for a semester (called monthly from background job)
    /// </summary>
    public async Task ProcessClubMembersAsync(int semesterId)
    {
        try
        {
            var clubs = await _context.Clubs
                .Include(c => c.Members)
                .Where(c => c.IsActive)
                .ToListAsync();

            foreach (var club in clubs)
            {
                var activeMembers = club.Members.Where(m => m.IsActive).ToList();
                
                foreach (var member in activeMembers)
                {
                    // Calculate score based on member contribution
                    // This would be filled in by club leader during monthly evaluation
                    // For now, we just set up the framework
                    _logger.LogInformation(
                        "Club member {StudentId} in club {ClubId} evaluated",
                        member.StudentId, club.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing club members");
        }
    }

    /// <summary>
    /// Calculate score for a club member based on their role and contribution
    /// Per Decision 414: President=10, VP=8, Manager=5, Member=3, Other=1
    /// </summary>
    public async Task<double> CalculateClubMemberScoreAsync(int studentId, int clubId, int monthYear)
    {
        try
        {
            var member = await _context.ClubMembers
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.ClubId == clubId && m.IsActive);

            if (member == null)
                return 0;

            // Calculate based on role
            double score = member.RoleInClub switch
            {
                "President" => 10,
                "VicePresident" => 8,
                "Manager" => 5,
                "Member" => 3,
                _ => 1
            };

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating club member score");
            return 0;
        }
    }

    /// <summary>
    /// Add club membership score to student's movement record
    /// </summary>
    public async Task AddClubMembershipScoreAsync(int studentId, int clubId, int semesterId, double score, string notes)
    {
        try
        {
            // Get criterion for club membership (Category 2)
            var criterion = await _context.MovementCriteria
                .FirstOrDefaultAsync(c => c.Title.Contains("CLB") && c.IsActive);

            if (criterion == null)
            {
                _logger.LogWarning("No criterion found for club membership");
                return;
            }

            // Get or create movement record
            var record = await _recordRepository.GetByStudentAndSemesterAsync(studentId, semesterId);
            if (record == null)
            {
                record = new MovementRecord
                {
                    StudentId = studentId,
                    SemesterId = semesterId,
                    TotalScore = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _recordRepository.CreateAsync(record);
            }

            // Check for duplicate in current month
            var month = DateTime.UtcNow.Month;
            var year = DateTime.UtcNow.Year;

            var existingDetail = await _context.MovementRecordDetails
                .Where(d => 
                    d.MovementRecordId == record.Id &&
                    d.CriterionId == criterion.Id &&
                    d.AwardedAt.Month == month &&
                    d.AwardedAt.Year == year)
                .FirstOrDefaultAsync();

            if (existingDetail != null)
            {
                _logger.LogInformation("Club score already added this month for student {StudentId}", studentId);
                return;
            }

            // Add score detail
            var detail = new MovementRecordDetail
            {
                MovementRecordId = record.Id,
                CriterionId = criterion.Id,
                Score = Math.Min(score, criterion.MaxScore),
                AwardedAt = DateTime.UtcNow
            };

            await _detailRepository.CreateAsync(detail);

            // Update record total
            var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
            record.TotalScore = Math.Min(totalScore, 100);
            record.LastUpdated = DateTime.UtcNow;
            await _recordRepository.UpdateAsync(record);

            _logger.LogInformation(
                "Added {Score} club membership points to student {StudentId}. Notes: {Notes}",
                score, studentId, notes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding club membership score");
        }
    }
}
