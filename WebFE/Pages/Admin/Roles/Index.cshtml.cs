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
    public List<ClubViewModel> AllClubs { get; set; } = new();
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

            // Get all clubs for ClubMember/ClubManager role assignment
            var clubsResponse = await client.GetAsync("/api/user-management/clubs");
            if (clubsResponse.IsSuccessStatusCode)
            {
                var content = await clubsResponse.Content.ReadAsStringAsync();
                AllClubs = JsonSerializer.Deserialize<List<ClubViewModel>>(content, new JsonSerializerOptions
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

    public async Task<IActionResult> OnPostUpdateRolesAsync(int userId, int[] roleIds, int? clubId)
    {
        var token = Request.Cookies["AccessToken"];
        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Take only the first role since users can only have one role now
            var roleId = roleIds?.FirstOrDefault() ?? 0;
            if (roleId == 0)
            {
                TempData["ErrorMessage"] = "Please select a role";
                return RedirectToPage();
            }

            var dto = new { UserId = userId, RoleId = roleId, ClubId = clubId };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"/api/user-management/{userId}/role", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "User role updated successfully";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                TempData["ErrorMessage"] = errorObj?.Message ?? "Failed to update role";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }

    private class ErrorResponse
    {
        public string? Message { get; set; }
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

public class ClubViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string SubName { get; set; } = null!;
}

