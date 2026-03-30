using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Models;
using PayrollManagementSystem.Services.Implementations;
using Xunit;

namespace Tests.Integration
{
    public class TimesheetServiceIntegrationTests
    {
        public static (AppDbContext context, SqliteConnection connection) HelperDbContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            return (context, connection);
        }

        private static Employee CreateTestEmployee(int employeeId = 1)
        {
            return new Employee
            {
                EmployeeId = employeeId,
                FullName = "Test Employee",
                Email = "test.employee@example.com",
                HourlyRate = 15,
                IsActive = true
            };
        }

        #region CreateOrGetTimesheetAsync Tests

        /// <summary>
        /// Ensures that when no timesheet exists for a given employee and week,
        /// a new timesheet is created, persisted to the database, and returned
        /// with default values.
        /// </summary>
        /// <returns>Non-null Timesheet</returns>
        [Fact]
        public async Task CreateOrGetTimesheetAsync_ShouldCreateNewTimesheet_WhenNoneExists()
        {
            var db = HelperDbContext();
            using var connection = db.connection;
            using var context = db.context;
            context.Employees.Add(CreateTestEmployee());
            await context.SaveChangesAsync();

            var service = new TimesheetService(context);
            var weekStart = new DateTime(2024, 6, 10);

            var timesheet = await service.CreateOrGetTimesheetAsync(1, weekStart);
            var saved = await context.Timesheets.FirstOrDefaultAsync();

            Assert.NotNull(timesheet);
            Assert.NotNull(saved);
            Assert.Equal(1, timesheet.EmployeeId);
            Assert.Equal(weekStart.Date, timesheet.WeekStart);
            Assert.Equal("Draft", timesheet.Status);
        }

        /// <summary>
        /// Ensures that when a timesheet already exists for a given employee and week,
        /// the existing record is returned and no duplicate timesheet is created.
        /// </summary>
        /// <returns>Timesheet matching the existing record</returns>
        [Fact]
        public async Task CreateOrGetTimesheetAsync_ShouldReturnExistingTimesheet_WhenAlreadyExists()
        {
            var db = HelperDbContext();
            using var context = db.context;
            using var connection = db.connection;

            context.Employees.Add(CreateTestEmployee());
            await context.SaveChangesAsync();

            var weekStart = new DateTime(2024, 6, 10);

            var existing = new Timesheet
            {
                EmployeeId = 1,
                WeekStart = weekStart.Date,
                Status = "Submitted"
            };

            context.Timesheets.Add(existing);
            await context.SaveChangesAsync();

            var service = new TimesheetService(context);

            var result = await service.CreateOrGetTimesheetAsync(1, weekStart);

            Assert.Equal(existing.TimesheetId, result.TimesheetId);
            Assert.Equal("Submitted", result.Status);
        }
        #endregion

        
    }
}