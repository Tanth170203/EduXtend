using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace WebFE.Pages.Admin.Students;

public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CreateModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public CreateStudentViewModel Student { get; set; } = new();
    
    public List<UserWithoutStudentInfo> UsersWithoutInfo { get; set; } = new();
    public List<MajorViewModel> Majors { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = Request.Cookies["AccessToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Get users without student info
            var usersResponse = await client.GetAsync("/api/students/users-without-info");
            if (usersResponse.IsSuccessStatusCode)
            {
                var content = await usersResponse.Content.ReadAsStringAsync();
                UsersWithoutInfo = JsonSerializer.Deserialize<List<UserWithoutStudentInfo>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }

            // Get majors
            var majorsResponse = await client.GetAsync("/api/majors");
            if (majorsResponse.IsSuccessStatusCode)
            {
                var content = await majorsResponse.Content.ReadAsStringAsync();
                Majors = JsonSerializer.Deserialize<List<MajorViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }

            // Set default values
            Student.EnrollmentDate = DateTime.Now;
            Student.DateOfBirth = DateTime.Now.AddYears(-20);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading data: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = Request.Cookies["AccessToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        if (!ModelState.IsValid)
        {
            return await OnGetAsync();
        }

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var json = JsonSerializer.Serialize(Student);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("/api/students", content);
            
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Student information created successfully";
                return RedirectToPage("./Index");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                var errorObj = JsonSerializer.Deserialize<ErrorResponse>(error, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Fallback: show status code and raw body if API didn't return { message }
                ErrorMessage = !string.IsNullOrWhiteSpace(errorObj?.Message)
                    ? errorObj!.Message
                    : $"Create failed: {(int)response.StatusCode} {response.ReasonPhrase} - {error}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }

        return await OnGetAsync();
    }
}

public class CreateStudentViewModel
{
    public int UserId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string Cohort { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public int Gender { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public int MajorId { get; set; }
    public int Status { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = null!;
}

