using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.Student
{
    public class MyPaymentsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int ClubId { get; set; }

        public IActionResult OnGet()
        {
            if (ClubId == 0)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }
    }
}
