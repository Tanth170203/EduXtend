using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace WebFE.Pages.Admin.Activities;

public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;

    public CreateModel(IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
    { _httpClientFactory = httpClientFactory; _env = env; }

    [BindProperty]
    public CreateActivityViewModel Activity { get; set; } = new();
    public List<string> ImageOptions { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        LoadImages();
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = Request.Cookies["AccessToken"]; if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var json = JsonSerializer.Serialize(Activity);
            var resp = await client.PostAsync("/api/activities", new StringContent(json, Encoding.UTF8, "application/json"));
            if (resp.IsSuccessStatusCode) return RedirectToPage("/Admin/Activities/Index");
            ErrorMessage = await resp.Content.ReadAsStringAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        LoadImages();
        return Page();
    }

    private void LoadImages()
    {
        var dir = Path.Combine(_env.WebRootPath, "images");
        if (Directory.Exists(dir))
        {
            ImageOptions = Directory.GetFiles(dir)
                .Select(p => "/images/" + Path.GetFileName(p))
                .ToList();
        }
    }
}

public class CreateActivityViewModel
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Now.AddDays(1);
    public DateTime EndTime { get; set; } = DateTime.Now.AddDays(1).AddHours(2);
    public int Type { get; set; } = 0;
    public bool IsPublic { get; set; } = true;
    public string? ImageUrl { get; set; }
}


