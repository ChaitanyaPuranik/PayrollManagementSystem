using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Areas.Identity.Data;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Models;
using PayrollManagementSystem.Services.Interfaces;
using PayrollManagementSystem.ViewModels.User.Timesheets;
using Microsoft.EntityFrameworkCore;

namespace PayrollManagementSystem.Pages.User.Timesheets
{
    [Authorize(Roles = "User")]
    public class EditModel : PageModel
    {
        private readonly ITimesheetService _timesheetService;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public EditModel(
            ITimesheetService timesheetService,
            UserManager<AppUser> userManager,
            AppDbContext context)
        {
            _timesheetService = timesheetService;
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public WeeklyTimesheetViewModel TimesheetVm { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(DateTime? weekStart)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
                return NotFound("Employee record not found for the logged-in user.");

            DateTime selectedWeekStart = NormalizeWeekStart(weekStart ?? DateTime.Today);

            var timesheet = await _timesheetService.CreateOrGetTimesheetAsync(employee.EmployeeId, selectedWeekStart);

            TimesheetVm = BuildViewModel(timesheet, employee.EmployeeId);

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
                return NotFound("Employee record not found for the logged-in user.");

            if (!ModelState.IsValid)
                return Page();

            var entries = TimesheetVm.Days.Select(d => new TimesheetEntry
            {
                WorkDate = d.WorkDate.Date,
                HoursWorked = d.HoursWorked,
                Notes = d.Notes
            }).ToList();

            await _timesheetService.SaveTimesheetEntriesAsync(TimesheetVm.TimesheetId, entries);

            TempData["SuccessMessage"] = "Timesheet saved successfully.";
            return RedirectToPage(new { weekStart = TimesheetVm.WeekStart.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostSubmitAsync()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
                return NotFound("Employee record not found for the logged-in user.");

            if (!ModelState.IsValid)
                return Page();

            var entries = TimesheetVm.Days.Select(d => new TimesheetEntry
            {
                WorkDate = d.WorkDate.Date,
                HoursWorked = d.HoursWorked,
                Notes = d.Notes
            }).ToList();

            await _timesheetService.SaveTimesheetEntriesAsync(TimesheetVm.TimesheetId, entries);
            await _timesheetService.SubmitTimesheetAsync(TimesheetVm.TimesheetId);

            TempData["SuccessMessage"] = "Timesheet submitted successfully.";
            return RedirectToPage(new { weekStart = TimesheetVm.WeekStart.ToString("yyyy-MM-dd") });
        }

        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return null;

            return await _context.Employees
                .FirstOrDefaultAsync(e => e.AppUserId == appUser.Id);
        }

        private static DateTime NormalizeWeekStart(DateTime date)
        {
            // Sunday-based week
            int diff = (int)date.DayOfWeek;
            return date.Date.AddDays(-diff);
        }

        private static WeeklyTimesheetViewModel BuildViewModel(Timesheet timesheet, int employeeId)
        {
            var vm = new WeeklyTimesheetViewModel
            {
                TimesheetId = timesheet.TimesheetId,
                EmployeeId = employeeId,
                WeekStart = timesheet.WeekStart.Date,
                Status = timesheet.Status
            };

            for (int i = 0; i < 7; i++)
            {
                var currentDate = timesheet.WeekStart.Date.AddDays(i);
                var existingEntry = timesheet.Entries.FirstOrDefault(e => e.WorkDate.Date == currentDate);

                vm.Days.Add(new TimesheetDayInputModel
                {
                    WorkDate = currentDate,
                    HoursWorked = existingEntry?.HoursWorked ?? 0,
                    Notes = existingEntry?.Notes
                });
            }

            return vm;
        }
    }
}