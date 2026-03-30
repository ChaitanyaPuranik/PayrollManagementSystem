using Xunit;
using PayrollManagementSystem.Services.Implementations;

namespace Tests
{
    public class PayrollServiceTests
    {
        [Fact]
        public void CalculateGrossPay_ShouldReturnCorrectGrossPay()
        {
            // Arrange
            var payrollService = new PayrollService(null!);
            decimal hoursWorked = 40;
            decimal hourlyRate = 15;

            // Act
            decimal grossPay = payrollService.CalculateGrossPay(hoursWorked, hourlyRate);

            // Assert
            Assert.Equal(600, grossPay);
        }

        [Fact]
        public void CalculateTax_ShouldReturnCorrectTax()
        {
            // Arrange
            var payrollService = new PayrollService(null!);
            decimal grossPay = 600;

            // Act
            var result = payrollService.CalculateTax(grossPay);

            // Assert
            Assert.Equal(78, result);
        }

        [Fact]
        public void CalculateNetPay_ShouldReturnCorrectNetPay()
        {
            // Arrange
            var payrollService = new PayrollService(null!);
            decimal grossPay = 600;

            // Act
            var result = payrollService.CalculateNetPay(grossPay);

            // Assert
            Assert.Equal(522, result);
        }
    }
}