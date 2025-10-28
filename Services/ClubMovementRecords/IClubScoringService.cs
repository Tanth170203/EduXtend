using BusinessObject.DTOs.ClubMovementRecord;

namespace Services.ClubMovementRecords;

public interface IClubScoringService
{
    // Query methods
    Task<ClubMovementRecordDto?> GetClubScoreAsync(int clubId, int semesterId, int month);
    Task<List<ClubMovementRecordDto>> GetAllClubScoresAsync(int semesterId, int month);

    // Manual scoring
    Task<ClubMovementRecordDto> AddManualScoreAsync(AddClubManualScoreDto dto);
    Task<ClubMovementRecordDto> UpdateManualScoreAsync(UpdateClubManualScoreDto dto, int adminUserId);
    Task DeleteManualScoreAsync(int detailId);
    
    // Plan scoring
    Task UpdatePlanScoreAsync(int clubId, int semesterId, int month, bool isCompleted);
}



