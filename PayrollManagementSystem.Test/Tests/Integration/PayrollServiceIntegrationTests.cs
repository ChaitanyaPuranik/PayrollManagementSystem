using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Services.Implementations;
using Xunit;

namespace Tests.Integration
{
    /// <summary>
    /// Integration tests for <see cref="PayrollService"/> against an in-memory SQLite database.
    /// Each test verifies results through a fresh context so it reads what was really persisted.
    /// </summary>
    public class PayrollServiceIntegrationTests
    {
        private static readonly DateTime Week1 = new DateTime(2024, 6, 10);
        private static readonly DateTime Week2 = new DateTime(2024, 6, 17);

        [Fact]
        public async Task RunPayrollAsync_ShouldCreatePayRunWithOneDetailPerSubmittedTimesheet()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddEmployee(2, 20m);
            db.AddTimesheet(1, Week1, "Submitted", 8, 8, 8, 8, 8);   // 40 h
            db.AddTimesheet(2, Week1, "Submitted", 5, 5, 5, 5);      // 20 h

            var payRunId = await new PayrollService(db.Context).RunPayrollAsync(Week1);

            using var verify = db.NewContext();
            var payRun = await verify.PayRuns.Include(p => p.PayDetails).SingleAsync();

            Assert.Equal(payRunId, payRun.PayRunId);
            Assert.Equal("Completed", payRun.Status);
            Assert.Equal(Week1, payRun.WeekStart);
            Assert.Equal(2, payRun.PayDetails.Count);

            var first = payRun.PayDetails.Single(d => d.EmployeeId == 1);
            Assert.Equal(600m, first.GrossPay);
            Assert.Equal(78m, first.TaxAmount);
            Assert.Equal(522m, first.NetPay);

            var second = payRun.PayDetails.Single(d => d.EmployeeId == 2);
            Assert.Equal(400m, second.GrossPay);
            Assert.Equal(52m, second.TaxAmount);
            Assert.Equal(348m, second.NetPay);
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldRoundPayToTwoDecimals()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 18.40m);
            db.AddTimesheet(1, Week1, "Submitted", 7.5m);

            await new PayrollService(db.Context).RunPayrollAsync(Week1);

            using var verify = db.NewContext();
            var detail = await verify.PayDetails.SingleAsync();

            Assert.Equal(138.00m, detail.GrossPay);
            Assert.Equal(17.94m, detail.TaxAmount);
            Assert.Equal(120.06m, detail.NetPay);
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldIgnoreTimeOfDay_WhenMatchingTheWeek()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddTimesheet(1, Week1, "Submitted", 8);

            await new PayrollService(db.Context).RunPayrollAsync(Week1.AddHours(9));

            using var verify = db.NewContext();
            var payRun = await verify.PayRuns.SingleAsync();

            Assert.Equal(Week1, payRun.WeekStart);
            Assert.Equal(1, await verify.PayDetails.CountAsync());
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldThrow_WhenPayrollAlreadyRunForTheWeek()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddTimesheet(1, Week1, "Submitted", 8);
            var service = new PayrollService(db.Context);

            await service.RunPayrollAsync(Week1);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunPayrollAsync(Week1));

            Assert.Contains("already been run", ex.Message);

            using var verify = db.NewContext();
            Assert.Equal(1, await verify.PayRuns.CountAsync());
            Assert.Equal(1, await verify.PayDetails.CountAsync());
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldThrow_WhenNoTimesheetIsSubmitted()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddTimesheet(1, Week1, "Draft", 8);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new PayrollService(db.Context).RunPayrollAsync(Week1));

            Assert.Contains("No submitted timesheets", ex.Message);

            using var verify = db.NewContext();
            Assert.Equal(0, await verify.PayRuns.CountAsync());
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldExcludeInactiveEmployees()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m, isActive: true);
            db.AddEmployee(2, 15m, isActive: false);
            db.AddTimesheet(1, Week1, "Submitted", 8);
            db.AddTimesheet(2, Week1, "Submitted", 8);

            await new PayrollService(db.Context).RunPayrollAsync(Week1);

            using var verify = db.NewContext();
            var detail = await verify.PayDetails.SingleAsync();

            Assert.Equal(1, detail.EmployeeId);
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldOnlyIncludeSubmittedTimesheetsForTheSelectedWeek()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddEmployee(2, 15m);
            db.AddEmployee(3, 15m);
            db.AddTimesheet(1, Week1, "Submitted", 8);   // included
            db.AddTimesheet(2, Week1, "Draft", 8);       // wrong status
            db.AddTimesheet(3, Week2, "Submitted", 8);   // wrong week

            await new PayrollService(db.Context).RunPayrollAsync(Week1);

            using var verify = db.NewContext();
            var detail = await verify.PayDetails.SingleAsync();

            Assert.Equal(1, detail.EmployeeId);
        }

        [Fact]
        public async Task RunPayrollAsync_ShouldRollBackEverything_WhenACalculationFails()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddEmployee(2, -1m);   // invalid rate makes the calculation throw mid-run
            db.AddTimesheet(1, Week1, "Submitted", 8);
            db.AddTimesheet(2, Week1, "Submitted", 8);

            await Assert.ThrowsAsync<ArgumentException>(
                () => new PayrollService(db.Context).RunPayrollAsync(Week1));

            using var verify = db.NewContext();
            Assert.Equal(0, await verify.PayRuns.CountAsync());
            Assert.Equal(0, await verify.PayDetails.CountAsync());
        }

        [Fact]
        public async Task GetPayrollHistoryForEmployeeAsync_ShouldReturnOnlyThatEmployeesDetails_NewestWeekFirst()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddEmployee(2, 15m);
            db.AddTimesheet(1, Week1, "Submitted", 8);
            db.AddTimesheet(2, Week1, "Submitted", 8);
            db.AddTimesheet(1, Week2, "Submitted", 8);

            var service = new PayrollService(db.Context);
            await service.RunPayrollAsync(Week1);
            await service.RunPayrollAsync(Week2);

            using var readContext = db.NewContext();
            var history = await new PayrollService(readContext).GetPayrollHistoryForEmployeeAsync(1);

            Assert.Equal(2, history.Count);
            Assert.All(history, d => Assert.Equal(1, d.EmployeeId));
            Assert.Equal(Week2, history[0].PayRun!.WeekStart);
            Assert.Equal(Week1, history[1].PayRun!.WeekStart);
        }

        [Fact]
        public async Task GetAllPayRunsAsync_ShouldReturnNewestWeekFirst()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddTimesheet(1, Week1, "Submitted", 8);
            db.AddTimesheet(1, Week2, "Submitted", 8);

            var service = new PayrollService(db.Context);
            await service.RunPayrollAsync(Week1);
            await service.RunPayrollAsync(Week2);

            using var readContext = db.NewContext();
            var payRuns = await new PayrollService(readContext).GetAllPayRunsAsync();

            Assert.Equal(new[] { Week2, Week1 }, payRuns.Select(p => p.WeekStart).ToArray());
        }

        [Fact]
        public async Task GetPayRunByIdAsync_ShouldIncludeDetailsAndEmployees()
        {
            using var db = new SqliteTestDatabase();
            db.AddEmployee(1, 15m);
            db.AddTimesheet(1, Week1, "Submitted", 8);
            var payRunId = await new PayrollService(db.Context).RunPayrollAsync(Week1);

            using var readContext = db.NewContext();
            var payRun = await new PayrollService(readContext).GetPayRunByIdAsync(payRunId);

            Assert.NotNull(payRun);
            var detail = Assert.Single(payRun!.PayDetails);
            Assert.Equal("Employee 1", detail.Employee!.FullName);
        }

        [Fact]
        public async Task GetPayRunByIdAsync_ShouldReturnNull_WhenPayRunDoesNotExist()
        {
            using var db = new SqliteTestDatabase();

            Assert.Null(await new PayrollService(db.Context).GetPayRunByIdAsync(999));
        }
    }
}
