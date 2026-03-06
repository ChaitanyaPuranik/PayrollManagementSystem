using System.ComponentModel.DataAnnotations;

namespace PayrollManagementSystem.Models
{
    public class PayRun
    {
        [Key]
        public int PayRunId { get; set; }

        [Required]
        public DateTime WeekStart { get; set; }

        [Required]
        public DateTime RunDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed";

        // Navigation property
        public ICollection<PayDetail> PayDetails { get; set; } = new List<PayDetail>();
    }
}