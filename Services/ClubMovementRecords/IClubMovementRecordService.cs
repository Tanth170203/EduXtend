using BusinessObject.Models;

namespace Services.ClubMovementRecords;

public interface IClubMovementRecordService
{
    /// <summary>
    /// Awards movement points to organizing and collaborating clubs when an activity is completed
    /// </summary>
    /// <param name="activity">The completed activity</param>
    /// <returns>Tuple containing points awarded to organizing club and collaborating club (if applicable)</returns>
    Task<(double organizingPoints, double? collaboratingPoints)> AwardActivityPointsAsync(Activity activity);
}
