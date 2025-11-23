namespace BusinessObject.DTOs.Activity
{
    public class AdminCreateActivityWithSchedulesRequest
    {
        public AdminCreateActivityDto Activity { get; set; } = null!;
        public List<CreateActivityScheduleDto> Schedules { get; set; } = new();
    }
}
