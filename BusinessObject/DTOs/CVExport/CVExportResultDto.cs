namespace BusinessObject.DTOs.CVExport
{
    public class CVExportResultDto
    {
        public int TotalRequests { get; set; }
        public int SuccessfullyParsed { get; set; }
        public int FailedToParse { get; set; }
        public List<ExtractedCVDataDto> ExtractedData { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
