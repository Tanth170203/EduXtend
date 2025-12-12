using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.Student
{
    [Authorize]
    public class MyFinanceModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
