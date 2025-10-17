using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace WebFE.Pages.Admin.Activities;

public class EditModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    public EditModel(IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
    { _httpClientFactory = httpClientFactory; _env = env; }

    [BindProperty]
    public ActivityEditVm Activity { get; set; } = new();
    public List<string> ImageOptions { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        LoadImages();
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var resp = await client.GetAsync($"/api/activities/{id}");
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { ErrorMessage = text; return Page(); }
            var vm = JsonSerializer.Deserialize<ActivityEditVm>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            Activity = vm;
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadImages();
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var json = JsonSerializer.Serialize(Activity);
            var resp = await client.PutAsync($"/api/activities/{Activity.Id}", new StringContent(json, Encoding.UTF8, "application/json"));
            if (resp.IsSuccessStatusCode) return RedirectToPage("/Admin/Activities/Detail", new { id = Activity.Id });
            ErrorMessage = await resp.Content.ReadAsStringAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        return Page();
    }

    private void LoadImages()
    {
        var dir = Path.Combine(_env.WebRootPath, "images");
        if (Directory.Exists(dir))
        {
            ImageOptions = Directory.GetFiles(dir).Select(p => "/images/" + Path.GetFileName(p)).ToList();
        }
    }
}

public class ActivityEditVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Type { get; set; }
    public bool IsPublic { get; set; }
    public string? ImageUrl { get; set; }
}


