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

        private const string AlreadyRunMessage = "Payroll has already been run for this week.";

        public async Task<int> RunPayrollAsync(DateTime weekStart)
        {
            DateTime normalizedWeekStart = weekStart.Date;

            // The SQL Server provider is configured with a retrying execution strategy, which
            // refuses user-initiated transactions unless the whole unit of work (checks, queries,
            // transaction) runs inside strategy.ExecuteAsync so it can be retried as one piece.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(() => RunPayrollInTransactionAsync(normalizedWeekStart));
        }

        private async Task<int> RunPayrollInTransactionAsync(DateTime normalizedWeekStart)
        {
            bool payrollAlreadyExists = await _context.PayRuns
                .AnyAsync(pr => pr.WeekStart == normalizedWeekStart);

            if (payrollAlreadyExists)
                throw new InvalidOperationException(AlreadyRunMessage);

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
            bool insertingPayRun = false;

            try
            {
                var payRun = new PayRun
                {
                    WeekStart = normalizedWeekStart,
                    RunDate = DateTime.UtcNow,
                    Status = "Completed"
                };

                _context.PayRuns.Add(payRun);

                insertingPayRun = true;
                await _context.SaveChangesAsync();
                insertingPayRun = false;

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
            catch (DbUpdateException) when (insertingPayRun)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                // The unique index on PayRun.WeekStart rejects a second run that slipped past the
                // check above (two admins generating the same week at the same moment).
                if (await _context.PayRuns.AnyAsync(pr => pr.WeekStart == normalizedWeekStart))
                    throw new InvalidOperationException(AlreadyRunMessage);

                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
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