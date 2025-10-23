using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.ClubManager
{
    public class InterviewDetailsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int RequestId { get; set; }

        public IActionResult OnGet()
        {
            // Just render the page, data will be loaded via JavaScript
            return Page();
        }
    }
}

