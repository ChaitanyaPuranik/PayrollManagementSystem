using PayrollManagementSystem.Models;

namespace PayrollManagementSystem.Services.Interfaces
{
    public interface IPayrollService
    {
        decimal CalculateGrossPay(decimal hoursWorked, decimal hourlyRate);
        decimal CalculateTax(decimal grossPay);
        decimal CalculateNetPay(decimal grossPay);

        Task<int> RunPayrollAsync(DateTime weekStart);

        Task<List<PayDetail>> GetPayrollHistoryForEmployeeAsync(int employeeId);
        Task<List<PayRun>> GetAllPayRunsAsync();
        Task<PayRun?> GetPayRunByIdAsync(int payRunId);
    }
}