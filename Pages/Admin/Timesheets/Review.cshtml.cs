using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Pages.Admin.Timesheets
{
    [Authorize(Roles = "Admin")]
    public class ReviewModel : PageModel
    {
        private readonly ITimesheetService _timesheetService;

        public ReviewModel(ITimesheetService timesheetService)
        {
            _timesheetService = timesheetService;
        }

        public TimesheetReviewViewModel? TimesheetVm { get; set; }

        public async Task<IActionResult> OnGetAsync(int timesheetId)
        {
            var timesheet = await _timesheetService.GetTimesheetWithDetailsAsync(timesheetId);
            if (timesheet == null)
                return NotFound();

            TimesheetVm = new TimesheetReviewViewModel
            {
                TimesheetId = timesheet.TimesheetId,
                EmployeeName = timesheet.Employee?.FullName ?? "N/A",
                EmployeeEmail = timesheet.Employee?.Email ?? "N/A",
                WeekStart = timesheet.WeekStart,
                WeekEnd = timesheet.WeekStart.AddDays(6),
                Status = timesheet.Status,
                SubmittedAt = timesheet.SubmittedAt,
                Entries = timesheet.Entries
                    .OrderBy(e => e.WorkDate)
                    .Select(e => new TimesheetEntryViewModel
                    {
                        WorkDate = e.WorkDate,
                        HoursWorked = e.HoursWorked,
                        Notes = e.Notes
                    })
                    .ToList()
            };

            return Page();
        }

        public class TimesheetReviewViewModel
        {
            public int TimesheetId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string EmployeeEmail { get; set; } = string.Empty;
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime? SubmittedAt { get; set; }
            public List<TimesheetEntryViewModel> Entries { get; set; } = new();
            public decimal TotalHours => Entries.Sum(e => e.HoursWorked);
        }

        public class TimesheetEntryViewModel
        {
            public DateTime WorkDate { get; set; }
            public decimal HoursWorked { get; set; }
            public string? Notes { get; set; }
        }
    }
}