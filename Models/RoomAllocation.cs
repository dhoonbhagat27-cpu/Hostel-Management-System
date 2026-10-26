using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class RoomAllocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        [Display(Name = "Bed")]
        public int BedId { get; set; }

        [ForeignKey("BedId")]
        public virtual Bed? Bed { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Allocation Date")]
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        [Display(Name = "Expected Vacating Date")]
        public DateTime? ExpectedVacatingDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Actual Vacating Date")]
        public DateTime? ActualVacatingDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // "Active", "Vacated", "Transferred", "Cancelled"

        [MaxLength(300)]
        public string? Remarks { get; set; }

        public string? AllocatedByUserId { get; set; }
    }
}
