using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages
{
    public class LoginModel : PageModel
    {
        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            // Check if user is already logged in
            var accessToken = Request.Cookies["accessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                // Redirect to home if already logged in
                Response.Redirect("/");
            }
        }

        public IActionResult OnGetLogout()
        {
            // Clear cookies
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

            // Call API to logout (optional - can be done via JavaScript)
            SuccessMessage = "Đăng xuất thành công";

            return RedirectToPage("/Index");
        }
    }
}

