using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Models;

namespace Tests.Integration
{
    /// <summary>
    /// An in-memory SQLite database for integration tests. All contexts created from one
    /// instance share the same connection, so a test can write with <see cref="Context"/>
    /// and verify with a fresh <see cref="NewContext"/> that has no tracked entities.
    /// </summary>
    public sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public AppDbContext Context { get; }

        public SqliteTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new AppDbContext(_options);
            Context.Database.EnsureCreated();
        }

        /// <summary>A new context over the same database, with an empty change tracker.</summary>
        public AppDbContext NewContext() => new AppDbContext(_options);

        public Employee AddEmployee(int id, decimal hourlyRate, bool isActive = true)
        {
            var employee = new Employee
            {
                EmployeeId = id,
                FullName = $"Employee {id}",
                Email = $"employee{id}@example.com",
                HourlyRate = hourlyRate,
                IsActive = isActive
            };

            Context.Employees.Add(employee);
            Context.SaveChanges();
            return employee;
        }

        /// <summary>
        /// Adds a timesheet with one entry per value in <paramref name="hoursPerDay"/>,
        /// on consecutive days starting at <paramref name="weekStart"/>.
        /// </summary>
        public Timesheet AddTimesheet(int employeeId, DateTime weekStart, string status, params decimal[] hoursPerDay)
        {
            var timesheet = new Timesheet
            {
                EmployeeId = employeeId,
                WeekStart = weekStart.Date,
                Status = status,
                SubmittedAt = status == "Submitted" ? DateTime.UtcNow : null,
                Entries = hoursPerDay
                    .Select((hours, i) => new TimesheetEntry
                    {
                        WorkDate = weekStart.Date.AddDays(i),
                        HoursWorked = hours
                    })
                    .ToList()
            };

            Context.Timesheets.Add(timesheet);
            Context.SaveChanges();
            return timesheet;
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
