using BusinessObject.DTOs.CVExport;
using BusinessObject.Models;
using Microsoft.Extensions.Logging;
using Repositories.JoinRequests;
using Repositories.Interviews;
using Repositories.Clubs;
using Repositories.Students;
using Microsoft.EntityFrameworkCore;

namespace Services.CVExport
{
    public class CVExportService : ICVExportService
    {
        private readonly IJoinRequestRepository _joinRequestRepo;
        private readonly IInterviewRepository _interviewRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly ICVDownloaderService _downloaderService;
        private readonly ICVParserService _parserService;
        private readonly IExcelGeneratorService _excelService;
        private readonly ILogger<CVExportService> _logger;

        public CVExportService(
            IJoinRequestRepository joinRequestRepo,
            IInterviewRepository interviewRepo,
            IClubRepository clubRepo,
            IStudentRepository studentRepo,
            ICVDownloaderService downloaderService,
            ICVParserService parserService,
            IExcelGeneratorService excelService,
            ILogger<CVExportService> logger)
        {
            _joinRequestRepo = joinRequestRepo;
            _interviewRepo = interviewRepo;
            _clubRepo = clubRepo;
            _studentRepo = studentRepo;
            _downloaderService = downloaderService;
            _parserService = parserService;
            _excelService = excelService;
            _logger = logger;
        }

        public async Task<CVExportResultDto> ExtractCVDataAsync(CVExportRequestDto request)
        {
            var result = new CVExportResultDto();

            try
            {
                _logger.LogInformation("Starting CV extraction for Club {ClubId}", request.ClubId);

                // Get unscheduled join requests
                var joinRequests = await GetUnscheduledRequestsAsync(request.ClubId);
                result.TotalRequests = joinRequests.Count;

                _logger.LogInformation("Found {Count} unscheduled join requests", joinRequests.Count);

                if (joinRequests.Count == 0)
                {
                    return result;
                }

                // Process each CV with limited parallelism
                var semaphore = new SemaphoreSlim(5); // Max 5 concurrent downloads
                var tasks = joinRequests.Select(async jr =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await ProcessSingleCVAsync(jr);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var extractedData = await Task.WhenAll(tasks);

                // Collect results
                foreach (var data in extractedData)
                {
                    result.ExtractedData.Add(data);
                    
                    if (data.ParseSuccess)
                    {
                        result.SuccessfullyParsed++;
                    }
                    else
                    {
                        result.FailedToParse++;
                        if (!string.IsNullOrEmpty(data.ParseError))
                        {
                            result.Errors.Add($"JoinRequest {data.JoinRequestId}: {data.ParseError}");
                        }
                    }
                }

                _logger.LogInformation("CV extraction completed. Success: {Success}, Failed: {Failed}", 
                    result.SuccessfullyParsed, result.FailedToParse);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CV extraction for Club {ClubId}", request.ClubId);
                result.Errors.Add($"System error: {ex.Message}");
                return result;
            }
        }

        public async Task<byte[]> GenerateExcelAsync(CVExportResultDto data, string clubName)
        {
            try
            {
                _logger.LogInformation("Generating Excel file for club: {Club}", clubName);
                
                var excelBytes = _excelService.GenerateExcel(data.ExtractedData, clubName);
                
                _logger.LogInformation("Excel file generated successfully, Size: {Size} bytes", excelBytes.Length);
                
                return excelBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file");
                throw;
            }
        }

        private async Task<List<JoinRequest>> GetUnscheduledRequestsAsync(int clubId)
        {
            try
            {
                // Get all pending join requests for the club
                var pendingRequests = await _joinRequestRepo.GetPendingByClubIdAsync(clubId);

                // Filter out requests that have interviews
                var unscheduledRequests = new List<JoinRequest>();
                
                foreach (var request in pendingRequests)
                {
                    var hasInterview = await _interviewRepo.ExistsForJoinRequestAsync(request.Id);
                    if (!hasInterview)
                    {
                        unscheduledRequests.Add(request);
                    }
                }

                _logger.LogInformation("Found {Count} unscheduled requests out of {Total} pending requests", 
                    unscheduledRequests.Count, pendingRequests.Count);

                return unscheduledRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unscheduled requests for Club {ClubId}", clubId);
                throw;
            }
        }

        private async Task<ExtractedCVDataDto> ProcessSingleCVAsync(JoinRequest joinRequest)
        {
            var result = new ExtractedCVDataDto
            {
                JoinRequestId = joinRequest.Id,
                CvUrl = joinRequest.CvUrl ?? string.Empty,
                SubmittedDate = joinRequest.CreatedAt
            };

            try
            {
                // Get student code from database
                var student = await _studentRepo.GetByUserIdAsync(joinRequest.UserId);
                if (student != null)
                {
                    result.StudentCode = student.StudentCode;
                }

                // Check if CV URL exists
                if (string.IsNullOrWhiteSpace(joinRequest.CvUrl))
                {
                    _logger.LogWarning("JoinRequest {Id} has no CV URL", joinRequest.Id);
                    result.ParseSuccess = false;
                    result.ParseError = "No CV URL provided";
                    return result;
                }

                // Download CV
                var downloadResult = await _downloaderService.DownloadCVAsync(joinRequest.CvUrl);
                if (downloadResult == null)
                {
                    _logger.LogWarning("Failed to download CV for JoinRequest {Id}", joinRequest.Id);
                    result.ParseSuccess = false;
                    result.ParseError = "Failed to download CV file";
                    return result;
                }

                var (fileData, extension) = downloadResult.Value;

                // Parse CV
                var parsedData = await _parserService.ParseCVAsync(fileData, extension, joinRequest.Id);
                
                // Merge parsed data with database data
                result.FullName = parsedData.FullName;
                result.Email = parsedData.Email;
                result.PhoneNumber = parsedData.PhoneNumber;
                result.Education = parsedData.Education;
                result.Experience = parsedData.Experience;
                result.Skills = parsedData.Skills;
                result.OtherInformation = parsedData.OtherInformation;
                result.ParseSuccess = parsedData.ParseSuccess;
                result.ParseError = parsedData.ParseError;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CV for JoinRequest {Id}", joinRequest.Id);
                result.ParseSuccess = false;
                result.ParseError = $"Error: {ex.Message}";
                return result;
            }
        }
    }
}
