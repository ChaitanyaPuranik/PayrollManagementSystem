using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Areas.Identity.Data;
using PayrollManagementSystem.Data;

namespace PayrollManagementSystem.Pages.User.Profile
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

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";

        public int EmployeeId { get; set; }
        public decimal HourlyRate { get; set; }
        public bool IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return;

            Email = appUser.Email ?? string.Empty;

            var roles = await _userManager.GetRolesAsync(appUser);
            Role = roles.FirstOrDefault() ?? "User";

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.AppUserId == appUser.Id);

            if (employee != null)
            {
                FullName = employee.FullName;
                EmployeeId = employee.EmployeeId;
                HourlyRate = employee.HourlyRate;
                IsActive = employee.IsActive;
            }
            else
            {
                FullName = appUser.Email ?? "N/A";
            }
        }
    }
}
