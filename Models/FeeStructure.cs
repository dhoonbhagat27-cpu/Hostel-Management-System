using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class FeeStructure
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Fee Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Display(Name = "Hostel Type")]
        public string HostelType { get; set; } = "All"; // "All", "Boys", "Girls"

        [Required]
        [MaxLength(30)]
        [Display(Name = "Room Type")]
        public string RoomType { get; set; } = "Double"; // "Single", "Double", "Triple", "Four-Sharing"

        [Required]
        [MaxLength(30)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = "2026-2027";

        [Required]
        [MaxLength(20)]
        [Display(Name = "Fee Term")]
        public string Term { get; set; } = "Semester"; // "Annual", "Semester", "Monthly"

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Hostel Rent")]
        public decimal HostelRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Mess Fee")]
        public decimal MessFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Maintenance Fee")]
        public decimal MaintenanceFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Security Deposit (Refundable)")]
        public decimal SecurityDeposit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
