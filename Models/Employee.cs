using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PayrollManagementSystem.Areas.Identity.Data;

namespace PayrollManagementSystem.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal HourlyRate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Link to Identity user
        public string? AppUserId { get; set; }

        public AppUser? AppUser { get; set; }

        // Navigation
        public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
        public ICollection<PayDetail> PayDetails { get; set; } = new List<PayDetail>();
    }
}