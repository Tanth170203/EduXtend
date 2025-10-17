using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebFE.Pages.Admin.Activities;

public class DetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public DetailModel(IHttpClientFactory httpClientFactory) { _httpClientFactory = httpClientFactory; }

    public ActivityVm Activity { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var resp = await client.GetAsync($"/api/activities/{id}");
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { ErrorMessage = text; return Page(); }
            Activity = JsonSerializer.Deserialize<ActivityVm>(text, new JsonSerializerOptions{PropertyNameCaseInsensitive=true}) ?? new();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        return Page();
    }
}

public class ActivityVm
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Type { get; set; }
    public bool IsPublic { get; set; }
    public string? ImageUrl { get; set; }
}


