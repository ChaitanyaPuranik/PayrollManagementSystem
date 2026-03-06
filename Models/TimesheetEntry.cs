using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollManagementSystem.Models
{
    public class TimesheetEntry
    {
        [Key]
        public int TimesheetEntryId { get; set; }

        [Required]
        public int TimesheetId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursWorked { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        // Navigation
        public Timesheet? Timesheet { get; set; }
    }
}