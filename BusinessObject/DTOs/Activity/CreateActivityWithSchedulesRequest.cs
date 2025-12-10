namespace BusinessObject.DTOs.Activity
{
    public class CreateActivityWithSchedulesRequest
    {
        public ClubCreateActivityDto Activity { get; set; } = null!;
        public List<CreateActivityScheduleDto> Schedules { get; set; } = new();
    }
}
