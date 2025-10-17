using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http.Headers;

namespace WebFE.Pages.Admin.Students;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IndexModel(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public List<StudentViewModel> Students { get; set; } = new();
    public List<UserWithoutStudentInfo> UsersWithoutInfo { get; set; } = new();
    public List<MajorViewModel> Majors { get; set; } = new();
    public string? SuccessMessage { get; set; }
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
            // Get all students
            var studentsResponse = await client.GetAsync("/api/students");
            if (studentsResponse.IsSuccessStatusCode)
            {
                var content = await studentsResponse.Content.ReadAsStringAsync();
                Students = JsonSerializer.Deserialize<List<StudentViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
            else
            {
                var error = await studentsResponse.Content.ReadAsStringAsync();
                ErrorMessage = $"Students API failed: {(int)studentsResponse.StatusCode} {studentsResponse.ReasonPhrase} - {error}";
            }

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
            else if (string.IsNullOrEmpty(ErrorMessage))
            {
                var error = await usersResponse.Content.ReadAsStringAsync();
                ErrorMessage = $"Users-without-info API failed: {(int)usersResponse.StatusCode} {usersResponse.ReasonPhrase} - {error}";
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
            else if (string.IsNullOrEmpty(ErrorMessage))
            {
                var error = await majorsResponse.Content.ReadAsStringAsync();
                ErrorMessage = $"Majors API failed: {(int)majorsResponse.StatusCode} {majorsResponse.ReasonPhrase} - {error}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading data: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var token = Request.Cookies["AccessToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.DeleteAsync($"/api/students/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Student deleted successfully";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Error deleting student: {error}";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }
}

public class StudentViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = null!;
    public string Cohort { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int Gender { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public int Status { get; set; }
    public int UserId { get; set; }
    public int MajorId { get; set; }
    public string? MajorName { get; set; }
    public string? MajorCode { get; set; }
}

public class UserWithoutStudentInfo
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}

public class MajorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
}

