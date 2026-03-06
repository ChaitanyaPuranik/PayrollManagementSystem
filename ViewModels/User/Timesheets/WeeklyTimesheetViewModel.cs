using System.ComponentModel.DataAnnotations;

namespace PayrollManagementSystem.ViewModels.User.Timesheets
{
    public class WeeklyTimesheetViewModel
    {
        public int TimesheetId { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        public DateTime WeekStart { get; set; }

        public string Status { get; set; } = "Draft";

        public List<TimesheetDayInputModel> Days { get; set; } = new();

        public decimal TotalHours => Days.Sum(d => d.HoursWorked);

        public DateTime PreviousWeekStart => WeekStart.AddDays(-7);
        public DateTime NextWeekStart => WeekStart.AddDays(7);
    }
}