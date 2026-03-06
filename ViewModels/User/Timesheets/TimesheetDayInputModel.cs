using System.ComponentModel.DataAnnotations;

namespace PayrollManagementSystem.ViewModels.User.Timesheets
{
    public class TimesheetDayInputModel
    {
        [Required]
        public DateTime WorkDate { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal HoursWorked { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        public string DayName => WorkDate.ToString("dddd");
        public string DisplayDate => WorkDate.ToString("MMM dd, yyyy");
    }
}