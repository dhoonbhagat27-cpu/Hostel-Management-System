using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Complaint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Electrical"; // Electrical, Plumbing, Cleaning, Water Supply, Furniture, Internet / Wi-Fi, Mess / Food, Other

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent

        [Required]
        [MaxLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Submitted"; // Submitted, In Progress, Resolved, Closed

        [Display(Name = "Submitted Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Resolved Date")]
        public DateTime? ResolvedDate { get; set; }

        [Display(Name = "Assigned Staff")]
        public int? AssignedStaffId { get; set; }

        [ForeignKey("AssignedStaffId")]
        public virtual Staff? AssignedStaff { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Resolution Remarks")]
        public string? ResolutionRemarks { get; set; }
    }
}
