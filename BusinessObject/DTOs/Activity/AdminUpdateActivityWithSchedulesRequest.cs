namespace BusinessObject.DTOs.Activity
{
    public class AdminUpdateActivityWithSchedulesRequest
    {
        public AdminUpdateActivityDto Activity { get; set; } = null!;
        public List<UpdateActivityScheduleDto> Schedules { get; set; } = new();
    }
}
