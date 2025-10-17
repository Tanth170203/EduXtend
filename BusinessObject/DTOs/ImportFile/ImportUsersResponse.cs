namespace BusinessObject.DTOs.ImportFile
{
    public class ImportUsersResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ImportUserError> Errors { get; set; } = new List<ImportUserError>();
        public List<string> SuccessMessages { get; set; } = new List<string>();
    }

    public class ImportUserError
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

