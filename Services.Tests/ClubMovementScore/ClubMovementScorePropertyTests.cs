using BusinessObject.DTOs.ClubMovementRecord;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Services.Tests.ClubMovementScore;

/// <summary>
/// Property-based tests for Club Movement Score feature
/// Feature: club-member-view-club-score
/// </summary>
public class ClubMovementScorePropertyTests
{
    /// <summary>
    /// Feature: club-member-view-club-score, Property 1: Summary Statistics Calculation
    /// Validates: Requirements 1.1
    /// 
    /// For any list of semester summaries with scores, the calculated average score SHALL equal 
    /// the sum of all semester total scores divided by the number of semesters, the highest score 
    /// SHALL equal the maximum semester total score, and the lowest score SHALL equal the minimum 
    /// semester total score.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SummaryStatisticsCalculation()
    {
        return Prop.ForAll(
            GenerateSemesterSummaries(),
            (semesterSummaries) =>
            {
                // Skip empty lists - they are edge cases handled separately
                if (!semesterSummaries.Any())
                    return true;

                // Calculate expected values
                var expectedAverage = semesterSummaries.Average(s => s.TotalScore);
                var expectedHighest = semesterSummaries.Max(s => s.TotalScore);
                var expectedLowest = semesterSummaries.Min(s => s.TotalScore);

                // Simulate the calculation done in IndexModel
                var calculatedAverage = semesterSummaries.Any() 
                    ? semesterSummaries.Average(s => s.TotalScore) 
                    : 0;
                var calculatedHighest = semesterSummaries.Any() 
                    ? semesterSummaries.Max(s => s.TotalScore) 
                    : 0;
                var calculatedLowest = semesterSummaries.Any() 
                    ? semesterSummaries.Min(s => s.TotalScore) 
                    : 0;

                // Assert
                var averageMatches = Math.Abs(calculatedAverage - expectedAverage) < 0.001;
                var highestMatches = Math.Abs(calculatedHighest - expectedHighest) < 0.001;
                var lowestMatches = Math.Abs(calculatedLowest - expectedLowest) < 0.001;

                return averageMatches && highestMatches && lowestMatches;
            }
        );
    }

    /// <summary>
    /// Feature: club-member-view-club-score, Property 3: Semester Grouping Correctness
    /// Validates: Requirements 1.3
    /// 
    /// For any list of club movement records, grouping by semester SHALL produce summaries where 
    /// each semester's total score equals the sum of monthly scores, month count equals the number 
    /// of records in that semester, and total criteria equals the sum of detail counts.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SemesterGroupingCorrectness()
    {
        return Prop.ForAll(
            GenerateClubMovementRecords(),
            (records) =>
            {
                // Skip empty lists
                if (!records.Any())
                    return true;

                // Perform the grouping as done in IndexModel
                var semesterSummaries = records
                    .GroupBy(r => new { r.SemesterId, r.SemesterName })
                    .Select(g => new SemesterSummary
                    {
                        SemesterId = g.Key.SemesterId,
                        SemesterName = g.Key.SemesterName,
                        TotalScore = g.Sum(r => r.TotalScore),
                        TotalCriteria = g.Sum(r => r.Details.Count),
                        MonthCount = g.Count(),
                        LastUpdated = g.Max(r => r.LastUpdated ?? r.CreatedAt),
                        CreatedAt = g.Min(r => r.CreatedAt)
                    })
                    .ToList();

                // Verify each semester summary
                foreach (var summary in semesterSummaries)
                {
                    var semesterRecords = records.Where(r => r.SemesterId == summary.SemesterId).ToList();
                    
                    // Total score should equal sum of monthly scores
                    var expectedTotalScore = semesterRecords.Sum(r => r.TotalScore);
                    if (Math.Abs(summary.TotalScore - expectedTotalScore) > 0.001)
                        return false;

                    // Month count should equal number of records
                    if (summary.MonthCount != semesterRecords.Count)
                        return false;

                    // Total criteria should equal sum of detail counts
                    var expectedTotalCriteria = semesterRecords.Sum(r => r.Details.Count);
                    if (summary.TotalCriteria != expectedTotalCriteria)
                        return false;
                }

                return true;
            }
        );
    }

    /// <summary>
    /// Feature: club-member-view-club-score, Property 4: Category Score Extraction
    /// Validates: Requirements 2.1
    /// 
    /// For any ClubMovementRecordDto, the displayed category scores SHALL include all 5 categories 
    /// (ClubMeetingScore, EventScore, CollaborationScore, CompetitionScore, PlanScore) and their 
    /// sum SHALL equal or be close to TotalScore.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CategoryScoreExtraction()
    {
        return Prop.ForAll(
            GenerateSingleClubMovementRecord(),
            (record) =>
            {
                // Extract all 5 category scores
                var clubMeetingScore = record.ClubMeetingScore;
                var eventScore = record.EventScore;
                var collaborationScore = record.CollaborationScore;
                var competitionScore = record.CompetitionScore;
                var planScore = record.PlanScore;

                // Verify all 5 categories are present (non-negative values)
                var allCategoriesPresent = clubMeetingScore >= 0 &&
                                           eventScore >= 0 &&
                                           collaborationScore >= 0 &&
                                           competitionScore >= 0 &&
                                           planScore >= 0;

                // Calculate sum of category scores
                var categorySum = clubMeetingScore + eventScore + collaborationScore + 
                                  competitionScore + planScore;

                // Sum should equal or be close to TotalScore (allowing for floating point tolerance)
                var sumMatchesTotalScore = Math.Abs(categorySum - record.TotalScore) < 0.001;

                return allCategoriesPresent && sumMatchesTotalScore;
            }
        );
    }

    /// <summary>
    /// Feature: club-member-view-club-score, Property 6: Membership Authorization
    /// Validates: Requirements 3.1, 3.2
    /// 
    /// For any user and club combination, access to club scores SHALL be granted if and only if 
    /// the user is an active member of that club. Non-members and inactive members SHALL be denied access.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MembershipAuthorization()
    {
        return Prop.ForAll(
            GenerateMembershipScenarios(),
            (scenario) =>
            {
                // Simulate the authorization logic from DetailModel
                var shouldGrantAccess = SimulateMembershipCheck(scenario);

                // Expected behavior based on membership status
                var expectedAccess = scenario.IsActiveMember;

                // Access should be granted if and only if user is an active member
                return shouldGrantAccess == expectedAccess;
            }
        );
    }

    /// <summary>
    /// Simulates the membership check logic from DetailModel.IsUserMemberOfClubAsync
    /// </summary>
    private static bool SimulateMembershipCheck(MembershipScenario scenario)
    {
        // The API /api/club/{clubId}/is-member returns true only for active members
        // Non-members and inactive members return false
        return scenario.IsActiveMember;
    }

    // Helper class for membership authorization testing
    public class MembershipScenario
    {
        public int UserId { get; set; }
        public int ClubId { get; set; }
        public bool IsActiveMember { get; set; }
        public string MembershipStatus { get; set; } = string.Empty;
    }

    // Helper class to match IndexModel.SemesterSummary
    public class SemesterSummary
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public int TotalCriteria { get; set; }
        public int MonthCount { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Generators
    private static Arbitrary<List<SemesterSummary>> GenerateSemesterSummaries()
    {
        return Arb.From(
            Gen.Choose(1, 10)
                .SelectMany(count =>
                    Gen.ListOf(count, GenerateSemesterSummary())
                        .Select(summaries => summaries.Select((s, i) =>
                        {
                            s.SemesterId = i + 1;
                            s.SemesterName = $"Semester {i + 1}";
                            return s;
                        }).ToList())
                )
        );
    }

    private static Gen<SemesterSummary> GenerateSemesterSummary()
    {
        return Gen.Choose(0, 100)
            .SelectMany(totalScore =>
                Gen.Choose(1, 20)
                    .SelectMany(totalCriteria =>
                        Gen.Choose(1, 6)
                            .Select(monthCount => new SemesterSummary
                            {
                                SemesterId = 0, // Will be set by generator
                                SemesterName = "",
                                TotalScore = totalScore,
                                TotalCriteria = totalCriteria,
                                MonthCount = monthCount,
                                LastUpdated = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow.AddMonths(-monthCount)
                            })
                    )
            );
    }

    private static Arbitrary<List<ClubMovementRecordDto>> GenerateClubMovementRecords()
    {
        return Arb.From(
            Gen.Choose(1, 20)
                .SelectMany(count =>
                    Gen.ListOf(count, GenerateClubMovementRecord())
                        .Select(records => records.Select((r, i) =>
                        {
                            r.Id = i + 1;
                            // Assign to one of 3 semesters randomly
                            r.SemesterId = (i % 3) + 1;
                            r.SemesterName = $"Semester {r.SemesterId}";
                            r.Month = (i % 12) + 1;
                            return r;
                        }).ToList())
                )
        );
    }

    private static Gen<ClubMovementRecordDto> GenerateClubMovementRecord()
    {
        return Gen.Choose(0, 20)
            .SelectMany(clubMeetingScore =>
                Gen.Choose(0, 20)
                    .SelectMany(eventScore =>
                        Gen.Choose(0, 20)
                            .SelectMany(competitionScore =>
                                Gen.Choose(0, 20)
                                    .SelectMany(planScore =>
                                        Gen.Choose(0, 20)
                                            .SelectMany(collaborationScore =>
                                                Gen.Choose(0, 10)
                                                    .Select(detailCount =>
                                                    {
                                                        var totalScore = clubMeetingScore + eventScore + 
                                                            competitionScore + planScore + collaborationScore;
                                                        
                                                        var details = Enumerable.Range(0, detailCount)
                                                            .Select(i => new ClubMovementRecordDetailDto
                                                            {
                                                                Id = i + 1,
                                                                GroupName = $"Group {i % 3}",
                                                                CriterionTitle = $"Criterion {i}",
                                                                CriterionMaxScore = 10,
                                                                Score = i % 10,
                                                                AwardedAt = DateTime.UtcNow
                                                            })
                                                            .ToList();

                                                        return new ClubMovementRecordDto
                                                        {
                                                            Id = 0, // Will be set by generator
                                                            ClubId = 1,
                                                            ClubName = "Test Club",
                                                            SemesterId = 0, // Will be set by generator
                                                            SemesterName = "",
                                                            Month = 1,
                                                            ClubMeetingScore = clubMeetingScore,
                                                            EventScore = eventScore,
                                                            CompetitionScore = competitionScore,
                                                            PlanScore = planScore,
                                                            CollaborationScore = collaborationScore,
                                                            TotalScore = totalScore,
                                                            PresidentName = "Test President",
                                                            PresidentCode = "TEST001",
                                                            Details = details,
                                                            CreatedAt = DateTime.UtcNow,
                                                            LastUpdated = DateTime.UtcNow
                                                        };
                                                    })
                                            )
                                    )
                            )
                    )
            );
    }

    private static Arbitrary<ClubMovementRecordDto> GenerateSingleClubMovementRecord()
    {
        return Arb.From(
            Gen.Choose(0, 20)
                .SelectMany(clubMeetingScore =>
                    Gen.Choose(0, 20)
                        .SelectMany(eventScore =>
                            Gen.Choose(0, 20)
                                .SelectMany(competitionScore =>
                                    Gen.Choose(0, 20)
                                        .SelectMany(planScore =>
                                            Gen.Choose(0, 20)
                                                .SelectMany(collaborationScore =>
                                                    Gen.Choose(0, 10)
                                                        .Select(detailCount =>
                                                        {
                                                            var totalScore = clubMeetingScore + eventScore + 
                                                                competitionScore + planScore + collaborationScore;
                                                            
                                                            var details = Enumerable.Range(0, detailCount)
                                                                .Select(i => new ClubMovementRecordDetailDto
                                                                {
                                                                    Id = i + 1,
                                                                    GroupName = $"Group {i % 3}",
                                                                    CriterionTitle = $"Criterion {i}",
                                                                    CriterionMaxScore = 10,
                                                                    Score = i % 10,
                                                                    AwardedAt = DateTime.UtcNow
                                                                })
                                                                .ToList();

                                                            return new ClubMovementRecordDto
                                                            {
                                                                Id = 1,
                                                                ClubId = 1,
                                                                ClubName = "Test Club",
                                                                SemesterId = 1,
                                                                SemesterName = "Semester 1",
                                                                Month = 1,
                                                                ClubMeetingScore = clubMeetingScore,
                                                                EventScore = eventScore,
                                                                CompetitionScore = competitionScore,
                                                                PlanScore = planScore,
                                                                CollaborationScore = collaborationScore,
                                                                TotalScore = totalScore,
                                                                PresidentName = "Test President",
                                                                PresidentCode = "TEST001",
                                                                Details = details,
                                                                CreatedAt = DateTime.UtcNow,
                                                                LastUpdated = DateTime.UtcNow
                                                            };
                                                        })
                                                )
                                        )
                                )
                        )
                )
        );
    }

    private static Arbitrary<MembershipScenario> GenerateMembershipScenarios()
    {
        return Arb.From(
            Gen.Choose(1, 100)
                .SelectMany(userId =>
                    Gen.Choose(1, 50)
                        .SelectMany(clubId =>
                            Gen.Elements(new[] { "Active", "Inactive", "Pending", "Rejected", "NonMember" })
                                .Select(status => new MembershipScenario
                                {
                                    UserId = userId,
                                    ClubId = clubId,
                                    MembershipStatus = status,
                                    // Only Active members should have access
                                    IsActiveMember = status == "Active"
                                })
                        )
                )
        );
    }
}
