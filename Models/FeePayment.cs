using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class FeePayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student Fee")]
        public int StudentFeeId { get; set; }

        [ForeignKey("StudentFeeId")]
        public virtual StudentFee? StudentFee { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Receipt Number")]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000.00)]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(30)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash"; // "Cash", "UPI", "Bank Transfer", "Demand Draft", "Card"

        [Required]
        [MaxLength(100)]
        [Display(Name = "Transaction Reference")]
        public string TransactionReference { get; set; } = string.Empty;

        [Required]
        public string RecordedByUserId { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Remarks { get; set; }
    }
}
