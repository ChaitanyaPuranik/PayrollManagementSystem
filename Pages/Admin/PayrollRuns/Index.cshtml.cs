using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Pages.Admin.PayrollRuns
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public IndexModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public List<PayRunItem> PayRuns { get; set; } = new();

        public async Task OnGetAsync()
        {
            var payRuns = await _payrollService.GetAllPayRunsAsync();

            PayRuns = payRuns
                .OrderByDescending(p => p.RunDate)
                .Select(p => new PayRunItem
                {
                    PayRunId = p.PayRunId,
                    WeekStart = p.WeekStart,
                    WeekEnd = p.WeekStart.AddDays(6),
                    RunDate = p.RunDate,
                    Status = p.Status
                })
                .ToList();
        }

        public class PayRunItem
        {
            public int PayRunId { get; set; }
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public DateTime RunDate { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}