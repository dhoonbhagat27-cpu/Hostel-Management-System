using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class StudentFee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Display(Name = "Fee Structure")]
        public int? FeeStructureId { get; set; }

        [ForeignKey("FeeStructureId")]
        public virtual FeeStructure? FeeStructure { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Fee Title")]
        public string FeeTitle { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Balance Amount")]
        public decimal BalanceAmount { get; set; }

        [Required]
        [MaxLength(25)]
        public string Status { get; set; } = "Unpaid"; // "Unpaid", "Partially Paid", "Paid", "Overdue"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();
    }
}
