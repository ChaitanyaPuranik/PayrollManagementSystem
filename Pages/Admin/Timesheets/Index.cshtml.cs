using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Pages.Admin.Timesheets
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ITimesheetService _timesheetService;

        public IndexModel(ITimesheetService timesheetService)
        {
            _timesheetService = timesheetService;
        }

        public List<SubmittedTimesheetItem> SubmittedTimesheets { get; set; } = new();

        public async Task OnGetAsync()
        {
            var timesheets = await _timesheetService.GetSubmittedTimesheetsAsync();

            SubmittedTimesheets = timesheets.Select(t => new SubmittedTimesheetItem
            {
                TimesheetId = t.TimesheetId,
                EmployeeName = t.Employee?.FullName ?? "N/A",
                EmployeeEmail = t.Employee?.Email ?? "N/A",
                WeekStart = t.WeekStart,
                WeekEnd = t.WeekStart.AddDays(6),
                Status = t.Status,
                SubmittedAt = t.SubmittedAt,
                TotalHours = t.Entries.Sum(e => e.HoursWorked)
            })
            .OrderByDescending(t => t.WeekStart)
            .ToList();
        }

        public class SubmittedTimesheetItem
        {
            public int TimesheetId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string EmployeeEmail { get; set; } = string.Empty;
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime? SubmittedAt { get; set; }
            public decimal TotalHours { get; set; }

            public string WeekStartDisplay => WeekStart.ToString("ddd yyyy.MM.dd");
            public string WeekEndDisplay => WeekEnd.ToString("ddd yyyy.MM.dd");
            public string SubmittedAtDisplay => SubmittedAt?.ToString("MMM dd, yyyy hh:mm tt") ?? "-";
        }
    }
}