using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Pages.Admin.PayrollRuns
{
    [Authorize(Roles = "Admin")]
    public class ReportModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public ReportModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public PayrollReportViewModel? ReportVm { get; set; }

        public async Task<IActionResult> OnGetAsync(int payRunId)
        {
            var payRun = await _payrollService.GetPayRunByIdAsync(payRunId);
            if (payRun == null)
                return NotFound();

            ReportVm = new PayrollReportViewModel
            {
                PayRunId = payRun.PayRunId,
                WeekStart = payRun.WeekStart,
                WeekEnd = payRun.WeekStart.AddDays(6),
                RunDate = payRun.RunDate,
                Status = payRun.Status,
                Details = payRun.PayDetails
                    .OrderBy(d => d.EmployeeId)
                    .Select(d => new PayrollDetailItem
                    {
                        EmployeeId = d.EmployeeId,
                        EmployeeName = d.Employee?.FullName ?? "N/A",
                        GrossPay = d.GrossPay,
                        TaxAmount = d.TaxAmount,
                        NetPay = d.NetPay
                    })
                    .ToList()
            };

            return Page();
        }

        public class PayrollReportViewModel
        {
            public int PayRunId { get; set; }
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public DateTime RunDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public List<PayrollDetailItem> Details { get; set; } = new();

            public decimal TotalGross => Details.Sum(x => x.GrossPay);
            public decimal TotalTax => Details.Sum(x => x.TaxAmount);
            public decimal TotalNet => Details.Sum(x => x.NetPay);
        }

        public class PayrollDetailItem
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public decimal GrossPay { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal NetPay { get; set; }
        }
    }
}