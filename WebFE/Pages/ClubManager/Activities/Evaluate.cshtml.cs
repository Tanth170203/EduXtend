using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class EvaluateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EvaluateModel> _logger;
        private readonly IConfiguration _config;

        public EvaluateModel(IHttpClientFactory httpClientFactory, ILogger<EvaluateModel> logger, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int ActivityId { get; set; }

        [BindProperty]
        public CreateActivityEvaluationDto Evaluation { get; set; } = new();

        public ActivityDetailDto? Activity { get; set; }
        public ActivityEvaluationDto? ExistingEvaluation { get; set; }
        public bool IsEditMode { get; set; }
        public string? ErrorMessage { get; set; }
        public string ApiBaseUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
                
                // Load activity details
                var client = _httpClientFactory.CreateClient("ApiClient");
                var activityRequest = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{ActivityId}");
                
                foreach (var cookie in Request.Cookies)
                {
                    activityRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var activityResponse = await client.SendAsync(activityRequest);
                
                if (!activityResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to load activity {Id}: {Status}", ActivityId, activityResponse.StatusCode);
                    return NotFound();
                }

                Activity = await activityResponse.Content.ReadFromJsonAsync<ActivityDetailDto>();
                
                if (Activity == null)
                {
                    return NotFound();
                }

                // Check if activity is completed
                if (Activity.Status != "Completed")
                {
                    TempData["ErrorMessage"] = "Only completed activities can be evaluated";
                    return RedirectToPage("./Details", new { id = ActivityId });
                }

                // Try to load existing evaluation (for edit mode)
                var evaluationRequest = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{ActivityId}/evaluation");
                
                foreach (var cookie in Request.Cookies)
                {
                    evaluationRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var evaluationResponse = await client.SendAsync(evaluationRequest);
                
                if (evaluationResponse.IsSuccessStatusCode)
                {
                    ExistingEvaluation = await evaluationResponse.Content.ReadFromJsonAsync<ActivityEvaluationDto>();
                    
                    if (ExistingEvaluation != null)
                    {
                        IsEditMode = true;
                        // Populate form with existing data
                        Evaluation = new CreateActivityEvaluationDto
                        {
                            ExpectedParticipants = ExistingEvaluation.ExpectedParticipants,
                            ActualParticipants = ExistingEvaluation.ActualParticipants,
                            Reason = ExistingEvaluation.Reason,
                            CommunicationScore = ExistingEvaluation.CommunicationScore,
                            OrganizationScore = ExistingEvaluation.OrganizationScore,
                            HostScore = ExistingEvaluation.HostScore,
                            SpeakerScore = ExistingEvaluation.SpeakerScore,
                            Success = ExistingEvaluation.Success,
                            Limitations = ExistingEvaluation.Limitations,
                            ImprovementMeasures = ExistingEvaluation.ImprovementMeasures
                        };
                    }
                }
                else
                {
                    // New evaluation - auto-fill from Activity data
                    // ExpectedParticipants: Use MaxParticipants if available, otherwise use RegisteredCount
                    Evaluation.ExpectedParticipants = Activity.MaxParticipants ?? Activity.RegisteredCount;
                    
                    // ActualParticipants: Use AttendedCount (number of people who actually attended)
                    Evaluation.ActualParticipants = Activity.AttendedCount;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evaluation form for activity {Id}", ActivityId);
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
            if (!ModelState.IsValid)
            {
                // Reload activity details
                await LoadActivityDetailsAsync();
                return Page();
            }

            try
            {
                // Validate reason if actual < expected
                if (Evaluation.ActualParticipants < Evaluation.ExpectedParticipants && 
                    string.IsNullOrWhiteSpace(Evaluation.Reason))
                {
                    ModelState.AddModelError("Evaluation.Reason", "Please provide a reason when actual participants is less than expected");
                    await LoadActivityDetailsAsync();
                    return Page();
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Determine if this is create or update
                var method = IsEditMode ? HttpMethod.Put : HttpMethod.Post;
                var request = new HttpRequestMessage(method, $"api/activity/{ActivityId}/evaluation");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var json = JsonSerializer.Serialize(Evaluation);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = IsEditMode 
                        ? "Evaluation updated successfully" 
                        : "Evaluation created successfully";
                    return RedirectToPage("./EvaluationDetail", new { activityId = ActivityId });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to save evaluation: {Error}", errorContent);
                    
                    // Try to parse error message
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorContent);
                        if (errorDoc.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            ErrorMessage = messageElement.GetString();
                        }
                        else
                        {
                            ErrorMessage = errorContent;
                        }
                    }
                    catch
                    {
                        ErrorMessage = errorContent;
                    }
                    
                    ModelState.AddModelError(string.Empty, ErrorMessage ?? "Failed to save evaluation. Please try again.");
                    await LoadActivityDetailsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving evaluation for activity {Id}", ActivityId);
                ModelState.AddModelError(string.Empty, "An error occurred while saving the evaluation");
                await LoadActivityDetailsAsync();
                return Page();
            }
        }

        private async Task LoadActivityDetailsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{ActivityId}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    Activity = await response.Content.ReadFromJsonAsync<ActivityDetailDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity details {Id}", ActivityId);
            }
        }
    }
}
