using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.ClubManager
{
    public class ClubInfoModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ClubInfoModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [BindProperty] public UpdateClubInfoDto Input { get; set; } = new();
        public List<CategoryItemDto> Categories { get; set; } = new();
        public string CurrentCategoryName { get; set; } = string.Empty;
        public bool CurrentIsActive { get; set; }
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
            var me = await client.GetFromJsonAsync<ClubDetailDto>("api/club/my-managed-club");
            if (me == null) return Redirect("/Error?code=404");
            ClubId = me.Id;
            Categories = await client.GetFromJsonAsync<List<CategoryItemDto>>("api/club/categories-lite") ?? new();
            CurrentCategoryName = me.CategoryName;
            CurrentIsActive = me.IsActive;
            Input = new UpdateClubInfoDto
            {
                Name = me.Name,
                SubName = me.SubName,
                Description = me.Description,
                LogoUrl = me.LogoUrl,
                BannerUrl = me.BannerUrl,
                CategoryId = null,
                IsActive = null
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var client = CreateAuthClient();
            var me = await client.GetFromJsonAsync<ClubDetailDto>("api/club/my-managed-club");
            if (me == null) return Redirect("/Error?code=404");
            var res = await client.PutAsJsonAsync($"api/club/{me.Id}", Input);
            if (!res.IsSuccessStatusCode)
            {
                TempData["Error"] = "Cập nhật không thành công";
            }
            else
            {
                TempData["Success"] = "Đã cập nhật thông tin CLB";
            }
            return RedirectToPage();
        }
    }
}


