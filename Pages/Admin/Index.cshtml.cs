using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ITimesheetService _timesheetService;
        private readonly IPayrollService _payrollService;

        public IndexModel(ITimesheetService timesheetService, IPayrollService payrollService)
        {
            _timesheetService = timesheetService;
            _payrollService = payrollService;
        }

        public int SubmittedTimesheetCount { get; set; }
        public decimal TotalSubmittedHours { get; set; }
        public string LastPayrollRunDisplay { get; set; } = "No payroll runs yet";

        public List<RecentSubmittedTimesheetItem> RecentSubmittedTimesheets { get; set; } = new();

        public async Task OnGetAsync()
        {
            var submittedTimesheets = await _timesheetService.GetSubmittedTimesheetsAsync();
            var payRuns = await _payrollService.GetAllPayRunsAsync();

            SubmittedTimesheetCount = submittedTimesheets.Count;
            TotalSubmittedHours = submittedTimesheets.Sum(t => t.Entries.Sum(e => e.HoursWorked));

            var latestPayRun = payRuns
                .OrderByDescending(p => p.RunDate)
                .FirstOrDefault();

            if (latestPayRun != null)
            {
                LastPayrollRunDisplay =
                    $"Week of {latestPayRun.WeekStart:MMM dd, yyyy} | Run on {latestPayRun.RunDate:MMM dd, yyyy}";
            }

            RecentSubmittedTimesheets = submittedTimesheets
                .OrderByDescending(t => t.SubmittedAt)
                .Take(5)
                .Select(t => new RecentSubmittedTimesheetItem
                {
                    TimesheetId = t.TimesheetId,
                    EmployeeName = t.Employee?.FullName ?? "N/A",
                    WeekStart = t.WeekStart,
                    WeekEnd = t.WeekStart.AddDays(6),
                    TotalHours = t.Entries.Sum(e => e.HoursWorked)
                })
                .ToList();
        }

        public class RecentSubmittedTimesheetItem
        {
            public int TimesheetId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public decimal TotalHours { get; set; }

            public string WeekStartDisplay => WeekStart.ToString("ddd yyyy.MM.dd");
            public string WeekEndDisplay => WeekEnd.ToString("ddd yyyy.MM.dd");
        }
    }
}