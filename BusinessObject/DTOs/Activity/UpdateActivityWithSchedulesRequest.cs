namespace BusinessObject.DTOs.Activity
{
    public class UpdateActivityWithSchedulesRequest
    {
        public ClubCreateActivityDto Activity { get; set; } = null!;
        public List<UpdateActivityScheduleDto> Schedules { get; set; } = new();
    }
}
