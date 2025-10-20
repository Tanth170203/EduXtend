using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http.Headers;

namespace WebFE.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<UserViewModel> Users { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var token = Request.Cookies["AccessToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync("/api/user-management");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Users = JsonSerializer.Deserialize<List<UserViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
        }
        catch { }

        return Page();
    }

    public async Task<IActionResult> OnPostBanAsync(int id)
    {
        var token = Request.Cookies["AccessToken"];
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync($"/api/user-management/{id}/ban", null);
            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "User banned successfully";
        }
        catch { }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnbanAsync(int id)
    {
        var token = Request.Cookies["AccessToken"];
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync($"/api/user-management/{id}/unban", null);
            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "User unbanned successfully";
        }
        catch { }

        return RedirectToPage();
    }
}

public class UserViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<RoleInfo> Roles { get; set; } = new();
}

public class RoleInfo
{
    public int Id { get; set; }
    public string RoleName { get; set; } = null!;
}

