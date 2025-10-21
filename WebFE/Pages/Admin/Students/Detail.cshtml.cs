using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebFE.Pages.Admin.Students;

public class DetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public StudentDetailViewModel? Student { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var token = Request.Cookies["AccessToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var resp = await client.GetAsync($"/api/students/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                ErrorMessage = $"Load failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}";
                return Page();
            }

            var json = await resp.Content.ReadAsStringAsync();
            Student = JsonSerializer.Deserialize<StudentDetailViewModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}

public class StudentDetailViewModel
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


