using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Models;
using PayrollManagementSystem.Services.Interfaces;

namespace PayrollManagementSystem.Services.Implementations
{
    public class TimesheetService : ITimesheetService
    {
        private readonly AppDbContext _context;

        public TimesheetService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Timesheet> CreateOrGetTimesheetAsync(int employeeId, DateTime weekStart)
        {
            DateTime normalizedWeekStart = weekStart.Date;

            var timesheet = await _context.Timesheets
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t =>
                    t.EmployeeId == employeeId &&
                    t.WeekStart == normalizedWeekStart);

            if (timesheet != null)
                return timesheet;

            timesheet = new Timesheet
            {
                EmployeeId = employeeId,
                WeekStart = normalizedWeekStart,
                Status = "Draft"
            };

            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            return timesheet;
        }

        public async Task<Timesheet?> GetTimesheetByIdAsync(int timesheetId)
        {
            return await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Entries.OrderBy(e => e.WorkDate))
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);
        }

        public async Task<Timesheet?> GetTimesheetForWeekAsync(int employeeId, DateTime weekStart)
        {
            DateTime normalizedWeekStart = weekStart.Date;

            return await _context.Timesheets
                .Include(t => t.Entries.OrderBy(e => e.WorkDate))
                .FirstOrDefaultAsync(t =>
                    t.EmployeeId == employeeId &&
                    t.WeekStart == normalizedWeekStart);
        }

        public async Task<List<Timesheet>> GetTimesheetsForEmployeeAsync(int employeeId)
        {
            return await _context.Timesheets
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.WeekStart)
                .ToListAsync();
        }

        public async Task SaveTimesheetEntriesAsync(int timesheetId, List<TimesheetEntry> entries)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);

            if (timesheet == null)
                throw new InvalidOperationException("Timesheet not found.");

            if (timesheet.Status != "Draft")
                throw new InvalidOperationException("Only draft timesheets can be edited.");

            foreach (var entry in entries)
            {
                if (entry.HoursWorked < 0 || entry.HoursWorked > 24)
                    throw new InvalidOperationException("Hours worked must be between 0 and 24.");
            }

            // Replace existing entries for simplicity
            _context.TimesheetEntries.RemoveRange(timesheet.Entries);

            foreach (var entry in entries)
            {
                entry.TimesheetId = timesheetId;
                _context.TimesheetEntries.Add(entry);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SubmitTimesheetAsync(int timesheetId)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);

            if (timesheet == null)
                throw new InvalidOperationException("Timesheet not found.");

            if (!timesheet.Entries.Any())
                throw new InvalidOperationException("Cannot submit an empty timesheet.");

            timesheet.Status = "Submitted";
            timesheet.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetWeeklyTotalHoursAsync(int timesheetId)
        {
            return await _context.TimesheetEntries
                .Where(e => e.TimesheetId == timesheetId)
                .SumAsync(e => e.HoursWorked);
        }
    }
}