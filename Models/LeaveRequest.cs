using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(300)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; } = string.Empty;

        [Display(Name = "Parent/Guardian Informed?")]
        public bool ParentInformed { get; set; } = true;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        public string? ReviewedByUserId { get; set; }

        [Display(Name = "Review Date")]
        public DateTime? ReviewDate { get; set; }

        [MaxLength(500)]
        [Display(Name = "Review Remarks")]
        public string? ReviewRemarks { get; set; }
    }
}
