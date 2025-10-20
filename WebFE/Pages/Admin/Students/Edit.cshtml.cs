using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace WebFE.Pages.Admin.Students;

public class EditModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EditModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public UpdateStudentViewModel Student { get; set; } = new();
    
    public List<MajorViewModel> Majors { get; set; } = new();
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
            // Get student
            var studentResponse = await client.GetAsync($"/api/students/{id}");
            if (studentResponse.IsSuccessStatusCode)
            {
                var content = await studentResponse.Content.ReadAsStringAsync();
                var student = JsonSerializer.Deserialize<StudentViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (student != null)
                {
                    Student.Id = student.Id;
                    Student.StudentCode = student.StudentCode;
                    Student.Cohort = student.Cohort;
                    Student.DateOfBirth = student.DateOfBirth;
                    Student.Gender = student.Gender;
                    Student.EnrollmentDate = student.EnrollmentDate;
                    Student.MajorId = student.MajorId;
                    Student.Status = student.Status;
                }
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

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var json = JsonSerializer.Serialize(Student);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync($"/api/students/{Student.Id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Student updated successfully";
                return RedirectToPage("./Index");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Error updating student: {error}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }

        return await OnGetAsync(Student.Id);
    }
}

public class UpdateStudentViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = null!;
    public string Cohort { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public int Gender { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public int MajorId { get; set; }
    public int Status { get; set; }
}

