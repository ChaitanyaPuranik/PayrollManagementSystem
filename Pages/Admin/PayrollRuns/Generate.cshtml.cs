using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Pages.Admin.PayrollRuns
{
    [Authorize(Roles = "Admin")]
    public class GenerateModel : PageModel
    {
        private readonly ITimesheetService _timesheetService;
        private readonly IPayrollService _payrollService;

        public GenerateModel(ITimesheetService timesheetService, IPayrollService payrollService)
        {
            _timesheetService = timesheetService;
            _payrollService = payrollService;
        }

        [BindProperty]
        public DateTime SelectedWeekStart { get; set; }

        public List<WeekPayrollCandidate> AvailableWeeks { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync(DateTime? weekStart)
        {
            SelectedWeekStart = weekStart?.Date ?? DateTime.Today.Date;
            await LoadAvailableWeeksAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAvailableWeeksAsync();

            try
            {
                var payRunId = await _payrollService.RunPayrollAsync(SelectedWeekStart.Date);
                SuccessMessage = $"Payroll generated successfully. Pay Run ID: {payRunId}";
                return RedirectToPage("/Admin/PayrollRuns/Report", new { payRunId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task LoadAvailableWeeksAsync()
        {
            var submitted = await _timesheetService.GetSubmittedTimesheetsAsync();

            AvailableWeeks = submitted
                .GroupBy(t => t.WeekStart.Date)
                .Select(g => new WeekPayrollCandidate
                {
                    WeekStart = g.Key,
                    WeekEnd = g.Key.AddDays(6),
                    SubmittedTimesheetCount = g.Count(),
                    TotalHours = g.Sum(t => t.Entries.Sum(e => e.HoursWorked))
                })
                .OrderByDescending(x => x.WeekStart)
                .ToList();
        }

        public class WeekPayrollCandidate
        {
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public int SubmittedTimesheetCount { get; set; }
            public decimal TotalHours { get; set; }
        }
    }
}