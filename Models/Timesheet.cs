using System.ComponentModel.DataAnnotations;

namespace PayrollManagementSystem.Models
{
    public class Timesheet
    {
        [Key]
        public int TimesheetId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        // Start of the week, e.g. Sunday or Monday depending on your rule
        [Required]
        public DateTime WeekStart { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        // Navigation
        public Employee? Employee { get; set; }
        public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    }
}