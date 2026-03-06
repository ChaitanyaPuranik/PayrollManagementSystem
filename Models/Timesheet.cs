using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollManagementSystem.Models
{
    public class Timesheet
    {
        [Key]
        public int TimesheetId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime WeekStart { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursWorked { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation property
        public Employee? Employee { get; set; }
    }
}