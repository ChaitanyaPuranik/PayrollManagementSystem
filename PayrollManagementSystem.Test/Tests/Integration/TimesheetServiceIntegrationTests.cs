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

        #region SaveTimesheetEntriesAsync Tests

        private static List<TimesheetEntry> Entries(DateTime weekStart, params decimal[] hoursPerDay) =>
            hoursPerDay
                .Select((hours, i) => new TimesheetEntry { WorkDate = weekStart.Date.AddDays(i), HoursWorked = hours })
                .ToList();

        [Fact]
        public async Task SaveTimesheetEntriesAsync_ShouldReplaceExistingEntries()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft", 8, 8);
            var service = new TimesheetService(db.Context);

            await service.SaveTimesheetEntriesAsync(timesheet.TimesheetId, Entries(new DateTime(2024, 6, 10), 6));

            using var verify = db.NewContext();
            var saved = await verify.TimesheetEntries.Where(e => e.TimesheetId == timesheet.TimesheetId).ToListAsync();

            var entry = Assert.Single(saved);
            Assert.Equal(6m, entry.HoursWorked);
        }

        [Theory]
        [InlineData(-0.5)]
        [InlineData(24.5)]
        public async Task SaveTimesheetEntriesAsync_ShouldThrow_WhenHoursAreOutOfRange(double hours)
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft");
            var service = new TimesheetService(db.Context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SaveTimesheetEntriesAsync(
                    timesheet.TimesheetId, Entries(new DateTime(2024, 6, 10), (decimal)hours)));

            Assert.Contains("between 0 and 24", ex.Message);
        }

        [Fact]
        public async Task SaveTimesheetEntriesAsync_ShouldAcceptBoundaryHours()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft");
            var service = new TimesheetService(db.Context);

            await service.SaveTimesheetEntriesAsync(
                timesheet.TimesheetId, Entries(new DateTime(2024, 6, 10), 0m, 24m));

            using var verify = db.NewContext();
            Assert.Equal(2, await verify.TimesheetEntries.CountAsync());
        }

        [Fact]
        public async Task SaveTimesheetEntriesAsync_ShouldThrow_WhenTwoEntriesShareAWorkDate()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft");
            var service = new TimesheetService(db.Context);

            var duplicateDates = new List<TimesheetEntry>
            {
                new TimesheetEntry { WorkDate = new DateTime(2024, 6, 10), HoursWorked = 4 },
                new TimesheetEntry { WorkDate = new DateTime(2024, 6, 10, 15, 0, 0), HoursWorked = 4 }
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SaveTimesheetEntriesAsync(timesheet.TimesheetId, duplicateDates));

            Assert.Contains("one entry per work date", ex.Message);
        }

        [Theory]
        [InlineData("Submitted")]
        [InlineData("Approved")]
        public async Task SaveTimesheetEntriesAsync_ShouldThrow_WhenTimesheetIsNotDraft(string status)
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), status, 8);
            var service = new TimesheetService(db.Context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SaveTimesheetEntriesAsync(timesheet.TimesheetId, Entries(new DateTime(2024, 6, 10), 4)));

            Assert.Contains("Only draft", ex.Message);

            using var verify = db.NewContext();
            Assert.Equal(8m, (await verify.TimesheetEntries.SingleAsync()).HoursWorked);
        }

        [Fact]
        public async Task SaveTimesheetEntriesAsync_ShouldThrow_WhenTimesheetDoesNotExist()
        {
            using var db = new SqliteTestDatabase();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new TimesheetService(db.Context).SaveTimesheetEntriesAsync(999, new List<TimesheetEntry>()));

            Assert.Contains("not found", ex.Message);
        }

        #endregion

        #region SubmitTimesheetAsync Tests

        [Fact]
        public async Task SubmitTimesheetAsync_ShouldMarkDraftAsSubmitted_AndStampSubmittedAt()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft", 8);
            var before = DateTime.UtcNow;

            await new TimesheetService(db.Context).SubmitTimesheetAsync(timesheet.TimesheetId);

            using var verify = db.NewContext();
            var saved = await verify.Timesheets.SingleAsync();

            Assert.Equal("Submitted", saved.Status);
            Assert.NotNull(saved.SubmittedAt);
            Assert.True(saved.SubmittedAt >= before.AddSeconds(-1));
        }

        [Fact]
        public async Task SubmitTimesheetAsync_ShouldThrow_WhenTimesheetHasNoEntries()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new TimesheetService(db.Context).SubmitTimesheetAsync(timesheet.TimesheetId));

            Assert.Contains("empty timesheet", ex.Message);

            using var verify = db.NewContext();
            Assert.Equal("Draft", (await verify.Timesheets.SingleAsync()).Status);
        }

        [Theory]
        [InlineData("Submitted")]
        [InlineData("Approved")]
        public async Task SubmitTimesheetAsync_ShouldThrow_WhenTimesheetIsNotDraft(string status)
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            var timesheet = db.AddTimesheet(1, new DateTime(2024, 6, 10), status, 8);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new TimesheetService(db.Context).SubmitTimesheetAsync(timesheet.TimesheetId));

            Assert.Contains("Only draft", ex.Message);
        }

        [Fact]
        public async Task SubmitTimesheetAsync_ShouldThrow_WhenTimesheetDoesNotExist()
        {
            using var db = new SqliteTestDatabase();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new TimesheetService(db.Context).SubmitTimesheetAsync(999));

            Assert.Contains("not found", ex.Message);
        }

        #endregion

        #region Query Tests

        [Fact]
        public async Task GetSubmittedTimesheetsAsync_ShouldReturnOnlySubmitted_MostRecentlySubmittedFirst()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddEmployee(2, 15m);
            db.AddEmployee(3, 15m);
            var older = db.AddTimesheet(1, new DateTime(2024, 6, 10), "Submitted", 8);
            var newer = db.AddTimesheet(2, new DateTime(2024, 6, 10), "Submitted", 8);
            db.AddTimesheet(3, new DateTime(2024, 6, 10), "Draft", 8);

            older.SubmittedAt = new DateTime(2024, 6, 11, 9, 0, 0, DateTimeKind.Utc);
            newer.SubmittedAt = new DateTime(2024, 6, 12, 9, 0, 0, DateTimeKind.Utc);
            await db.Context.SaveChangesAsync();

            using var readContext = db.NewContext();
            var result = await new TimesheetService(readContext).GetSubmittedTimesheetsAsync();

            Assert.Equal(new[] { newer.TimesheetId, older.TimesheetId }, result.Select(t => t.TimesheetId).ToArray());
        }

        [Fact]
        public async Task GetTimesheetsForEmployeeAsync_ShouldReturnOnlyThatEmployeesTimesheets_NewestWeekFirst()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddEmployee(2, 15m);
            db.AddTimesheet(1, new DateTime(2024, 6, 10), "Draft");
            db.AddTimesheet(1, new DateTime(2024, 6, 17), "Draft");
            db.AddTimesheet(2, new DateTime(2024, 6, 24), "Draft");

            using var readContext = db.NewContext();
            var result = await new TimesheetService(readContext).GetTimesheetsForEmployeeAsync(1);

            Assert.Equal(
                new[] { new DateTime(2024, 6, 17), new DateTime(2024, 6, 10) },
                result.Select(t => t.WeekStart).ToArray());
        }

        [Fact]
        public async Task GetTimesheetForWeekAsync_ShouldReturnNull_WhenNoneExists()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);

            Assert.Null(await new TimesheetService(db.Context)
                .GetTimesheetForWeekAsync(1, new DateTime(2024, 6, 10)));
        }

        #endregion

        
    }
}