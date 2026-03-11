using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Areas.Identity.Data;
using PayrollManagementSystem.Data;

namespace PayrollManagementSystem.Pages.User
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public IndexModel(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<RecentTimesheetItem> RecentTimesheets { get; set; } = new();

        public async Task OnGetAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.AppUserId == appUser.Id);

            if (employee == null)
                return;

            RecentTimesheets = await _context.Timesheets
                .Where(t => t.EmployeeId == employee.EmployeeId)
                .OrderByDescending(t => t.WeekStart)
                .Take(5)
                .Select(t => new RecentTimesheetItem
                {
                    WeekStart = t.WeekStart,
                    Status = t.Status
                })
                .ToListAsync();

            foreach (var item in RecentTimesheets)
            {
                item.WeekEnd = item.WeekStart.AddDays(6);
            }
        }

        public class RecentTimesheetItem
        {
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public string Status { get; set; } = string.Empty;

            public string WeekStartDisplay => WeekStart.ToString("ddd yyyy.MM.dd");
            public string WeekEndDisplay => WeekEnd.ToString("ddd yyyy.MM.dd");
        }
    }
}
