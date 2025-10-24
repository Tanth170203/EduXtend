using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.ClubManager
{
    public class MembersModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public MembersModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public List<ClubMemberManageItemDto> Members { get; private set; } = new();
        [BindProperty(SupportsGet = true)] public int ClubId { get; set; }

        private HttpClient CreateAuthClient()
        {
            var token = Request.Cookies["AccessToken"];
            var client = _httpClientFactory.CreateClient("ApiClient");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var client = CreateAuthClient();
            // If ClubId not provided, try to resolve from My Managed Club
            if (ClubId == 0)
            {
                var me = await client.GetFromJsonAsync<ClubDetailDto>("api/club/my-managed-club");
                if (me == null) return Redirect("/Error?code=404");
                ClubId = me.Id;
            }
            Members = await client.GetFromJsonAsync<List<ClubMemberManageItemDto>>($"api/club/{ClubId}/members/manage") ?? new();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateRoleAsync(int studentId, string role)
        {
            var client = CreateAuthClient();
            var res = await client.PutAsJsonAsync($"api/club/{ClubId}/members/{studentId}/role", role);
            return RedirectToPage(new { ClubId });
        }

        public async Task<IActionResult> OnPostRemoveAsync(int studentId)
        {
            var client = CreateAuthClient();
            var res = await client.DeleteAsync($"api/club/{ClubId}/members/{studentId}");
            return RedirectToPage(new { ClubId });
        }
    }
}


