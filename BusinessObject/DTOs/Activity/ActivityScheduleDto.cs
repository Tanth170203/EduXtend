namespace BusinessObject.DTOs.Activity
{
    public class ActivityScheduleDto
    {
        public int Id { get; set; }
        public string StartTime { get; set; } = null!; // HH:mm format
        public string EndTime { get; set; } = null!; // HH:mm format
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public List<ActivityScheduleAssignmentDto> Assignments { get; set; } = new();
    }
}
