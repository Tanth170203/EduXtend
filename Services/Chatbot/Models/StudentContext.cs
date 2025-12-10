namespace Services.Chatbot.Models
{
    public class StudentContext
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MajorName { get; set; } = string.Empty;
        public string Cohort { get; set; } = string.Empty;
        public List<string> CurrentClubs { get; set; } = new List<string>();
        public List<string> Interests { get; set; } = new List<string>();
    }
}
