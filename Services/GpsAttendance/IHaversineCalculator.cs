namespace Services.GpsAttendance;

/// <summary>
/// Interface for calculating distance between two GPS coordinates using Haversine formula
/// </summary>
public interface IHaversineCalculator
{
    /// <summary>
    /// Calculates the distance between two GPS coordinates using the Haversine formula
    /// </summary>
    /// <param name="lat1">Latitude of first point in degrees</param>
    /// <param name="lon1">Longitude of first point in degrees</param>
    /// <param name="lat2">Latitude of second point in degrees</param>
    /// <param name="lon2">Longitude of second point in degrees</param>
    /// <returns>Distance in meters</returns>
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
}
