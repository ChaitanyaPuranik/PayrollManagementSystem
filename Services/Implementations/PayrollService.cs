using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Models;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Services.Implementations
{
    public class PayrollService : IPayrollService
    {
        private readonly AppDbContext _context;
        private const decimal TaxRate = 0.13m;

        public PayrollService(AppDbContext context)
        {
            _context = context;
        }

        public decimal CalculateGrossPay(decimal hoursWorked, decimal hourlyRate)
        {
            if (hoursWorked < 0)
                throw new ArgumentException("Hours worked cannot be negative.");

            if (hourlyRate < 0)
                throw new ArgumentException("Hourly rate cannot be negative.");

            return Math.Round(hoursWorked * hourlyRate, 2);
        }

        public decimal CalculateTax(decimal grossPay)
        {
            if (grossPay < 0)
                throw new ArgumentException("Gross pay cannot be negative.");

            return Math.Round(grossPay * TaxRate, 2);
        }

        public decimal CalculateNetPay(decimal grossPay)
        {
            if (grossPay < 0)
                throw new ArgumentException("Gross pay cannot be negative.");

            decimal tax = CalculateTax(grossPay);
            return Math.Round(grossPay - tax, 2);
        }

        public async Task<int> RunPayrollAsync(DateTime weekStart)
        {
            DateTime normalizedWeekStart = weekStart.Date;

            bool payrollAlreadyExists = await _context.PayRuns
                .AnyAsync(pr => pr.WeekStart == normalizedWeekStart);

            if (payrollAlreadyExists)
                throw new InvalidOperationException("Payroll has already been run for this week.");

            var timesheets = await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Entries)
                .Where(t => t.WeekStart == normalizedWeekStart
                            && t.Employee != null
                            && t.Employee.IsActive
                            && t.Status == "Submitted")
                .ToListAsync();

            if (!timesheets.Any())
                throw new InvalidOperationException("No submitted timesheets found for the selected week.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var payRun = new PayRun
                {
                    WeekStart = normalizedWeekStart,
                    RunDate = DateTime.UtcNow,
                    Status = "Completed"
                };

                _context.PayRuns.Add(payRun);
                await _context.SaveChangesAsync();

                foreach (var timesheet in timesheets)
                {
                    decimal totalHours = timesheet.Entries.Sum(e => e.HoursWorked);
                    decimal grossPay = CalculateGrossPay(totalHours, timesheet.Employee!.HourlyRate);
                    decimal taxAmount = CalculateTax(grossPay);
                    decimal netPay = CalculateNetPay(grossPay);

                    var payDetail = new PayDetail
                    {
                        PayRunId = payRun.PayRunId,
                        EmployeeId = timesheet.EmployeeId,
                        GrossPay = grossPay,
                        TaxAmount = taxAmount,
                        NetPay = netPay
                    };

                    _context.PayDetails.Add(payDetail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return payRun.PayRunId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PayDetail>> GetPayrollHistoryForEmployeeAsync(int employeeId)
        {
            return await _context.PayDetails
                .Include(pd => pd.PayRun)
                .Where(pd => pd.EmployeeId == employeeId)
                .OrderByDescending(pd => pd.PayRun!.WeekStart)
                .ToListAsync();
        }

        public async Task<List<PayRun>> GetAllPayRunsAsync()
        {
            return await _context.PayRuns
                .OrderByDescending(pr => pr.WeekStart)
                .ToListAsync();
        }

        public async Task<PayRun?> GetPayRunByIdAsync(int payRunId)
        {
            return await _context.PayRuns
                .Include(pr => pr.PayDetails)
                    .ThenInclude(pd => pd.Employee)
                .FirstOrDefaultAsync(pr => pr.PayRunId == payRunId);
        }
    }
}