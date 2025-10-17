using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebFE.Pages.Admin.Activities;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public IndexModel(IHttpClientFactory httpClientFactory) { _httpClientFactory = httpClientFactory; }

    public List<ActivityViewModel> Activities { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var resp = await client.GetAsync("/api/activities/admin");
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { ErrorMessage = text; return Page(); }
            Activities = JsonSerializer.Deserialize<List<ActivityViewModel>>(text, new JsonSerializerOptions{PropertyNameCaseInsensitive=true}) ?? new();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var resp = await client.DeleteAsync($"/api/activities/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = await resp.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        return await OnGetAsync();
    }
}

public class ActivityViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Type { get; set; }
    public bool IsPublic { get; set; }
    public string? ImageUrl { get; set; }
}


