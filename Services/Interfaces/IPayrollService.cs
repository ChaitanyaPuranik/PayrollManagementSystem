using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Models;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Services.Implementations
{
    public class PayrollService : IPayrollService
    {
        private readonly AppDbContext _context;

        // You can later move this to config/appsettings
        private const decimal TaxRate = 0.13m;

        public PayrollService(AppDbContext context)
        {
            _context = context;
        }

        public decimal CalculateGrossPay(decimal hoursWorked, decimal hourlyRate)
        {
            return hoursWorked * hourlyRate;
        }

        public decimal CalculateTax(decimal grossPay)
        {
            return grossPay * TaxRate;
        }

        public decimal CalculateNetPay(decimal grossPay)
        {
            var tax = CalculateTax(grossPay);
            return grossPay - tax;
        }

        public async Task<int> RunPayrollAsync(DateTime weekStart)
        {
            // Check if a payroll run already exists for this week
            var existingRun = await _context.PayRuns
                .FirstOrDefaultAsync(pr => pr.WeekStart.Date == weekStart.Date);

            if (existingRun != null)
            {
                throw new InvalidOperationException("Payroll has already been run for this week.");
            }

            var timesheets = await _context.Timesheets
                .Include(t => t.Employee)
                .Where(t => t.WeekStart.Date == weekStart.Date && t.Employee != null && t.Employee.IsActive)
                .ToListAsync();

            if (!timesheets.Any())
            {
                throw new InvalidOperationException("No timesheets found for the selected week.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var payRun = new PayRun
                {
                    WeekStart = weekStart.Date,
                    RunDate = DateTime.UtcNow,
                    Status = "Completed"
                };

                _context.PayRuns.Add(payRun);
                await _context.SaveChangesAsync();

                foreach (var timesheet in timesheets)
                {
                    var gross = CalculateGrossPay(timesheet.HoursWorked, timesheet.Employee!.HourlyRate);
                    var tax = CalculateTax(gross);
                    var net = CalculateNetPay(gross);

                    var payDetail = new PayDetail
                    {
                        PayRunId = payRun.PayRunId,
                        EmployeeId = timesheet.EmployeeId,
                        GrossPay = gross,
                        TaxAmount = tax,
                        NetPay = net
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
                .OrderByDescending(pd => pd.PayRun!.RunDate)
                .ToListAsync();
        }

        public async Task<List<PayRun>> GetAllPayRunsAsync()
        {
            return await _context.PayRuns
                .OrderByDescending(pr => pr.RunDate)
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