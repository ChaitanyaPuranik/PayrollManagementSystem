using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollManagementSystem.Models
{
    public class PayDetail
    {
        [Key]
        public int PayDetailId { get; set; }

        [Required]
        public int PayRunId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal GrossPay { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal NetPay { get; set; }

        // Navigation properties
        public PayRun? PayRun { get; set; }
        public Employee? Employee { get; set; }
    }
}