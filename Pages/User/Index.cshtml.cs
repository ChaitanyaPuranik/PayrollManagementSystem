using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PayrollManagementSystem.Pages.User
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        public string CurrentTimesheetStatus { get; set; } = "Draft";
        public decimal TotalHoursThisWeek { get; set; } = 0;
        public string LatestNetPayDisplay { get; set; } = "$0.00";

        public void OnGet()
        {
            // Later:
            // - get logged-in user
            // - load employee
            // - load latest timesheet
            // - load latest payroll
        }
    }
}