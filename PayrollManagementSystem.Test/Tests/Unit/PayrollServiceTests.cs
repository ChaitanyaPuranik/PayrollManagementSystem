using Xunit;
using PayrollManagementSystem.Services.Implementations;

namespace Tests
{
    /// <summary>
    /// Unit tests for the pure pay calculations. These never touch the database,
    /// so the service is created without a context.
    /// </summary>
    public class PayrollServiceTests
    {
        private static PayrollService CreateService() => new PayrollService(null!);

        // hours, hourly rate, expected gross pay
        public static IEnumerable<object[]> GrossPayCases => new List<object[]>
        {
            new object[] { 40m, 15m, 600m },
            new object[] { 0m, 15m, 0m },
            new object[] { 40m, 0m, 0m },
            new object[] { 7.5m, 18.40m, 138.00m },
            new object[] { 3.333m, 7m, 23.33m },   // 23.331 rounds down
            new object[] { 3.336m, 7m, 23.35m },   // 23.352 rounds up
        };

        // gross pay, expected tax at the flat 13% rate
        public static IEnumerable<object[]> TaxCases => new List<object[]>
        {
            new object[] { 600m, 78m },
            new object[] { 0m, 0m },
            new object[] { 1000m, 130m },
            new object[] { 138m, 17.94m },
            new object[] { 10.01m, 1.30m },        // 1.3013 rounds down
        };

        // gross pay, expected net pay
        public static IEnumerable<object[]> NetPayCases => new List<object[]>
        {
            new object[] { 600m, 522m },
            new object[] { 0m, 0m },
            new object[] { 138m, 120.06m },
            new object[] { 10.01m, 8.71m },
        };

        [Theory]
        [MemberData(nameof(GrossPayCases))]
        public void CalculateGrossPay_ShouldReturnHoursTimesRate_RoundedToTwoDecimals(
            decimal hours, decimal rate, decimal expected)
        {
            Assert.Equal(expected, CreateService().CalculateGrossPay(hours, rate));
        }

        [Theory]
        [InlineData(-1.0, 15.0)]
        [InlineData(-0.01, 15.0)]
        public void CalculateGrossPay_ShouldThrow_WhenHoursAreNegative(double hours, double rate)
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CreateService().CalculateGrossPay((decimal)hours, (decimal)rate));

            Assert.Contains("Hours", ex.Message);
        }

        [Fact]
        public void CalculateGrossPay_ShouldThrow_WhenRateIsNegative()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CreateService().CalculateGrossPay(40m, -15m));

            Assert.Contains("rate", ex.Message);
        }

        [Theory]
        [MemberData(nameof(TaxCases))]
        public void CalculateTax_ShouldApplyThirteenPercent_RoundedToTwoDecimals(decimal gross, decimal expected)
        {
            Assert.Equal(expected, CreateService().CalculateTax(gross));
        }

        [Fact]
        public void CalculateTax_ShouldThrow_WhenGrossPayIsNegative()
        {
            Assert.Throws<ArgumentException>(() => CreateService().CalculateTax(-0.01m));
        }

        [Theory]
        [MemberData(nameof(NetPayCases))]
        public void CalculateNetPay_ShouldReturnGrossMinusTax(decimal gross, decimal expected)
        {
            Assert.Equal(expected, CreateService().CalculateNetPay(gross));
        }

        [Fact]
        public void CalculateNetPay_ShouldThrow_WhenGrossPayIsNegative()
        {
            Assert.Throws<ArgumentException>(() => CreateService().CalculateNetPay(-0.01m));
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(99.99)]
        [InlineData(600.0)]
        [InlineData(1234.56)]
        public void NetPayPlusTax_ShouldAlwaysEqualGrossPay(double grossValue)
        {
            var service = CreateService();
            decimal gross = (decimal)grossValue;

            Assert.Equal(gross, service.CalculateNetPay(gross) + service.CalculateTax(gross));
        }
    }
}
