namespace Services.GpsAttendance;

/// <summary>
/// Calculates distance between two GPS coordinates using the Haversine formula
/// </summary>
public class HaversineCalculator : IHaversineCalculator
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculates the distance between two GPS coordinates using the Haversine formula
    /// </summary>
    /// <param name="lat1">Latitude of first point in degrees</param>
    /// <param name="lon1">Longitude of first point in degrees</param>
    /// <param name="lat2">Latitude of second point in degrees</param>
    /// <param name="lon2">Longitude of second point in degrees</param>
    /// <returns>Distance in meters</returns>
    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert to radians
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // Return distance in meters
        return EarthRadiusKm * c * 1000;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
