using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PayrollManagementSystem.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet() {
            return Redirect("/Identity/Account/Register");
        }
    }
}
