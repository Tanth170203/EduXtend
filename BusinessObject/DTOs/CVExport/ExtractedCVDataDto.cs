namespace BusinessObject.DTOs.CVExport
{
    public class ExtractedCVDataDto
    {
        public int JoinRequestId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string OtherInformation { get; set; } = string.Empty;
        public string CvUrl { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public bool ParseSuccess { get; set; }
        public string ParseError { get; set; } = string.Empty;
    }
}
