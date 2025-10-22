using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.Student
{
    public class ProfileModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(IHttpClientFactory http, ILogger<ProfileModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        [BindProperty]
        public UpdateProfileRequest Form { get; set; } = new();

        public ProfileDto? Profile { get; set; }

        // Extra display fields to match UI
        public string? StudentId { get; set; }
        public string? MajorName { get; set; }
        public string? Cohort { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public List<string>? Roles { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var token = Request.Cookies["AccessToken"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _http.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                // Load profile
                var resp = await client.GetAsync("/api/profile");
                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = "Failed to load profile.";
                    return Page();
                }

                var json = await resp.Content.ReadAsStringAsync();
                Profile = JsonSerializer.Deserialize<ProfileDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (Profile != null)
                {
                    Form.FullName = Profile.FullName;
                    Form.AvatarUrl = Profile.AvatarUrl;
                    Form.PhoneNumber = Profile.PhoneNumber;
                }

                // Load current user with student info for display (avatar sync)
                var meResp = await client.GetAsync("/api/auth/me");
                if (meResp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await meResp.Content.ReadAsStringAsync());
                    var root = doc.RootElement;
                    Roles = root.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array
                        ? rolesEl.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                        : new List<string>();
                    if (root.TryGetProperty("student", out var st) && st.ValueKind == JsonValueKind.Object)
                    {
                        StudentId = st.TryGetProperty("studentId", out var v0) ? v0.GetString() : null;
                        Cohort = st.TryGetProperty("cohort", out var v1) ? v1.GetString() : null;
                        MajorName = st.TryGetProperty("major", out var v2) ? v2.GetString() : null;
                        Gender = st.TryGetProperty("gender", out var v3) ? v3.GetString() : null;
                        if (st.TryGetProperty("dateOfBirth", out var v4) && v4.ValueKind != JsonValueKind.Null)
                        {
                            if (DateTime.TryParse(v4.ToString(), out var dob))
                                DateOfBirth = dob;
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile");
                ErrorMessage = "An error occurred while loading profile.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }

            try
            {
                var token = Request.Cookies["AccessToken"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _http.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var payload = new StringContent(JsonSerializer.Serialize(Form), Encoding.UTF8, "application/json");
                var resp = await client.PutAsync("/api/profile", payload);
                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = "Failed to update profile.";
                    return await OnGetAsync();
                }

                SuccessMessage = "Profile updated successfully.";

                // Refresh navbar avatar/name
                if (HttpContext.Response != null)
                {
                    // Trigger client-side refresh
                    Response.Headers["X-Trigger-Auth-UI"] = "1";
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile");
                ErrorMessage = "An error occurred while updating profile.";
                return await OnGetAsync();
            }
        }
    }
}


