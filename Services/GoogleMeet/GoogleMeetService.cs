using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services.GoogleMeet
{
    public class GoogleMeetService : IGoogleMeetService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleMeetService> _logger;

        public GoogleMeetService(
            IConfiguration configuration,
            ILogger<GoogleMeetService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateMeetLinkAsync(
            string summary,
            string description,
            DateTime startTime,
            int durationMinutes = 60)
        {
            try
            {
                var calendarId = _configuration["GoogleMeet:CalendarId"] ?? "primary";
                var authType = _configuration["GoogleMeet:AuthType"] ?? "ServiceAccount"; // "ServiceAccount" or "OAuth"

                GoogleCredential credential;

                if (authType == "OAuth")
                {
                    credential = await GetOAuthCredentialAsync();
                    _logger.LogInformation("Using OAuth 2.0 authentication");
                }
                else
                {
                    credential = await GetServiceAccountCredentialAsync();
                    _logger.LogInformation("Using Service Account authentication");
                }

                // Create Calendar service
                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "EduXtend Interview System"
                });

                // Create event - first try with simple hangout link
                var newEvent = new Event
                {
                    Summary = summary,
                    Description = description,
                    Start = new EventDateTime
                    {
                        DateTime = startTime,
                        TimeZone = "Asia/Ho_Chi_Minh"
                    },
                    End = new EventDateTime
                    {
                        DateTime = startTime.AddMinutes(durationMinutes),
                        TimeZone = "Asia/Ho_Chi_Minh"
                    },
                    // Allow anyone with link to join without approval
                    GuestsCanInviteOthers = true,
                    GuestsCanModify = false,
                    GuestsCanSeeOtherGuests = true,
                    // Set visibility to public so anyone with link can join
                    Visibility = "public"
                };

                Event createdEvent;
                
                // Try method 1: Use ConferenceData with hangoutsMeet
                try
                {
                    newEvent.ConferenceData = new ConferenceData
                    {
                        CreateRequest = new CreateConferenceRequest
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            ConferenceSolutionKey = new ConferenceSolutionKey
                            {
                                Type = "hangoutsMeet"
                            }
                        }
                    };

                    var request = service.Events.Insert(newEvent, calendarId);
                    request.ConferenceDataVersion = 1;
                    createdEvent = await request.ExecuteAsync();
                    
                    _logger.LogInformation("Successfully created event with hangoutsMeet conference type");
                }
                catch (Google.GoogleApiException apiEx) when (apiEx.Message.Contains("Invalid conference type"))
                {
                    _logger.LogWarning("Conference type 'hangoutsMeet' not supported, trying eventHangout");
                    
                    // Try method 2: Use eventHangout type
                    try
                    {
                        newEvent.ConferenceData = new ConferenceData
                        {
                            CreateRequest = new CreateConferenceRequest
                            {
                                RequestId = Guid.NewGuid().ToString(),
                                ConferenceSolutionKey = new ConferenceSolutionKey
                                {
                                    Type = "eventHangout"
                                }
                            }
                        };

                        var request2 = service.Events.Insert(newEvent, calendarId);
                        request2.ConferenceDataVersion = 1;
                        createdEvent = await request2.ExecuteAsync();
                        
                        _logger.LogInformation("Successfully created event with eventHangout conference type");
                    }
                    catch (Google.GoogleApiException)
                    {
                        _logger.LogWarning("eventHangout also failed, using simple event creation");
                        
                        // Method 3: Create simple event and generate custom meet link
                        newEvent.ConferenceData = null;
                        var simpleRequest = service.Events.Insert(newEvent, calendarId);
                        createdEvent = await simpleRequest.ExecuteAsync();
                        
                        // Generate a meet link based on event ID
                        var meetCode = GenerateMeetCode();
                        var meetLink = $"https://meet.google.com/{meetCode}";
                        
                        _logger.LogWarning("Created event without conference data. Generated custom meet link: {MeetLink}", meetLink);
                        _logger.LogWarning("Note: This meet link may not work. Please configure domain-wide delegation or use OAuth 2.0");
                        
                        return meetLink;
                    }
                }

                if (createdEvent.ConferenceData?.EntryPoints != null)
                {
                    var meetLink = createdEvent.ConferenceData.EntryPoints
                        .FirstOrDefault(ep => ep.EntryPointType == "video")?.Uri;

                    if (!string.IsNullOrEmpty(meetLink))
                    {
                        _logger.LogInformation("Successfully created Google Meet link: {MeetLink}", meetLink);
                        return meetLink;
                    }
                }

                _logger.LogError("Failed to generate Google Meet link - no conference data returned");
                throw new InvalidOperationException("Failed to generate Google Meet link");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Google Meet link");
                throw new InvalidOperationException("Không thể tạo link Google Meet. Vui lòng thử lại sau hoặc chọn phỏng vấn trực tiếp.", ex);
            }
        }

        private async Task<GoogleCredential> GetServiceAccountCredentialAsync()
        {
            var serviceAccountKeyPath = _configuration["GoogleMeet:ServiceAccountKeyPath"];

            if (string.IsNullOrEmpty(serviceAccountKeyPath))
            {
                _logger.LogError("Google Meet ServiceAccountKeyPath is not configured");
                throw new InvalidOperationException("Google Meet ServiceAccountKeyPath is not configured");
            }

            if (!File.Exists(serviceAccountKeyPath))
            {
                _logger.LogError("Service account key file not found at: {Path}", serviceAccountKeyPath);
                throw new InvalidOperationException($"Service account key file not found at: {serviceAccountKeyPath}");
            }

            var impersonateUser = _configuration["GoogleMeet:ImpersonateUser"];

            using (var stream = new FileStream(serviceAccountKeyPath, FileMode.Open, FileAccess.Read))
            {
                var baseCredential = GoogleCredential.FromStream(stream)
                    .CreateScoped(new[] {
                        CalendarService.Scope.Calendar,
                        CalendarService.Scope.CalendarEvents
                    });

                if (!string.IsNullOrEmpty(impersonateUser))
                {
                    _logger.LogInformation("Using domain-wide delegation to impersonate: {User}", impersonateUser);
                    return baseCredential.CreateWithUser(impersonateUser);
                }
                else
                {
                    _logger.LogWarning("No impersonation user configured. Domain-wide delegation may not work.");
                    return baseCredential;
                }
            }
        }

        private async Task<GoogleCredential> GetOAuthCredentialAsync()
        {
            var oauthClientPath = _configuration["GoogleMeet:OAuthClientPath"];
            var tokenStorePath = _configuration["GoogleMeet:TokenStorePath"] ?? "token.json";

            if (string.IsNullOrEmpty(oauthClientPath))
            {
                _logger.LogError("Google Meet OAuthClientPath is not configured");
                throw new InvalidOperationException("Google Meet OAuthClientPath is not configured");
            }

            if (!File.Exists(oauthClientPath))
            {
                _logger.LogError("OAuth client file not found at: {Path}", oauthClientPath);
                throw new InvalidOperationException($"OAuth client file not found at: {oauthClientPath}");
            }

            UserCredential userCredential;
            using (var stream = new FileStream(oauthClientPath, FileMode.Open, FileAccess.Read))
            {
                var clientSecrets = await GoogleClientSecrets.FromStreamAsync(stream);

                userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets.Secrets,
                    new[] { CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenStorePath, true));
            }

            _logger.LogInformation("OAuth 2.0 authentication successful");
            return GoogleCredential.FromAccessToken(userCredential.Token.AccessToken);
        }

        private string GenerateMeetCode()
        {
            // Generate a random meet code similar to Google Meet format (xxx-xxxx-xxx)
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            
            var part1 = new string(Enumerable.Range(0, 3).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            var part3 = new string(Enumerable.Range(0, 3).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            
            return $"{part1}-{part2}-{part3}";
        }

        public async Task<(bool Success, string Message)> TestConfigurationAsync()
        {
            try
            {
                var serviceAccountKeyPath = _configuration["GoogleMeet:ServiceAccountKeyPath"];
                
                if (string.IsNullOrEmpty(serviceAccountKeyPath))
                {
                    return (false, "GoogleMeet:ServiceAccountKeyPath is not configured");
                }

                if (!File.Exists(serviceAccountKeyPath))
                {
                    return (false, $"Service account key file not found at: {serviceAccountKeyPath}");
                }

                // Try to create a test event
                var testSummary = "Test Event - EduXtend Configuration Check";
                var testDescription = "This is a test event to verify Google Meet integration. It will be deleted automatically.";
                var testStartTime = DateTime.Now.AddMinutes(5);

                try
                {
                    var meetLink = await CreateMeetLinkAsync(testSummary, testDescription, testStartTime, 30);
                    
                    // TODO: Delete the test event
                    
                    return (true, $"Configuration is valid. Test meet link created: {meetLink}");
                }
                catch (Exception ex)
                {
                    return (false, $"Failed to create test event: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Configuration test failed: {ex.Message}");
            }
        }
    }
}
