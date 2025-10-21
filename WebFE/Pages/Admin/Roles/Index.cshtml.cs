using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace WebFE.Pages.Admin.Roles;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<UserWithRolesViewModel> Users { get; set; } = new();
    public List<RoleViewModel> AllRoles { get; set; } = new();
    public int TotalUsers { get; set; }
    public int AdminCount { get; set; }
    public int StudentCount { get; set; }
    public int ClubManagerCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = Request.Cookies["AccessToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Get all users with roles
            var usersResponse = await client.GetAsync("/api/user-management");
            if (usersResponse.IsSuccessStatusCode)
            {
                var content = await usersResponse.Content.ReadAsStringAsync();
                Users = JsonSerializer.Deserialize<List<UserWithRolesViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }

            // Get all available roles
            var rolesResponse = await client.GetAsync("/api/user-management/roles");
            if (rolesResponse.IsSuccessStatusCode)
            {
                var content = await rolesResponse.Content.ReadAsStringAsync();
                AllRoles = JsonSerializer.Deserialize<List<RoleViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
        }
        catch { }

        // Calculate statistics
        TotalUsers = Users.Count;
        AdminCount = Users.Count(u => u.Roles.Any(r => r.RoleName == "Admin"));
        StudentCount = Users.Count(u => u.Roles.Any(r => r.RoleName == "Student"));
        ClubManagerCount = Users.Count(u => u.Roles.Any(r => r.RoleName == "ClubManager"));

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateRolesAsync(int userId, int[] roleIds)
    {
        var token = Request.Cookies["AccessToken"];
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var dto = new { UserId = userId, RoleIds = roleIds?.ToList() ?? new List<int>() };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"/api/user-management/{userId}/roles", content);
            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "User roles updated successfully";
            else
                TempData["ErrorMessage"] = "Failed to update roles";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }
}

public class UserWithRolesViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<RoleViewModel> Roles { get; set; } = new();
    public List<int> RoleIds { get; set; } = new();
}

public class RoleViewModel
{
    public int Id { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
}

