namespace BusinessObject.DTOs.Activity
{
    public class AdminActivityRegistrantDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool? IsPresent { get; set; }
        public int? ParticipationScore { get; set; }
        
        // GPS Attendance fields
        public string? CheckInMethod { get; set; }
        public DateTime? GpsCheckInTime { get; set; }
        public DateTime? GpsCheckOutTime { get; set; }
        public double? CheckInDistance { get; set; }
    }
}




