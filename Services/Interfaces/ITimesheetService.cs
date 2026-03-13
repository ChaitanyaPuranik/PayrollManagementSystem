using PayrollManagementSystem.Models;

namespace PayrollManagementSystem.Services.Interfaces
{
    public interface ITimesheetService
    {
        Task<Timesheet> CreateOrGetTimesheetAsync(int employeeId, DateTime weekStart);

        Task<Timesheet?> GetTimesheetByIdAsync(int timesheetId);

        Task<Timesheet?> GetTimesheetForWeekAsync(int employeeId, DateTime weekStart);

        Task<List<Timesheet>> GetTimesheetsForEmployeeAsync(int employeeId);

        Task SaveTimesheetEntriesAsync(int timesheetId, List<TimesheetEntry> entries);

        Task SubmitTimesheetAsync(int timesheetId);

        Task<decimal> GetWeeklyTotalHoursAsync(int timesheetId);

        Task<List<Timesheet>> GetSubmittedTimesheetsAsync();
        Task<Timesheet?> GetTimesheetWithDetailsAsync(int timesheetId);
    }
}