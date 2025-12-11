namespace Services.GoogleMeet
{
    public interface IGoogleMeetService
    {
        /// <summary>
        /// Creates a Google Meet link for an interview
        /// </summary>
        /// <param name="summary">Meeting title</param>
        /// <param name="description">Meeting description</param>
        /// <param name="startTime">Meeting start time</param>
        /// <param name="durationMinutes">Meeting duration in minutes</param>
        /// <returns>Google Meet link URL</returns>
        Task<string> CreateMeetLinkAsync(
            string summary,
            string description,
            DateTime startTime,
            int durationMinutes = 60);

        /// <summary>
        /// Tests the Google Meet configuration
        /// </summary>
        /// <returns>Tuple with success status and message</returns>
        Task<(bool Success, string Message)> TestConfigurationAsync();
    }
}
