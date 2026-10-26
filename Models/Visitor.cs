using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Visitor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student Visited")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Visitor Name")]
        public string VisitorName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Display(Name = "Relation to Student")]
        public string RelationToStudent { get; set; } = "Parent"; // Parent, Sibling, Relative, Friend, Other

        [Required]
        [MaxLength(200)]
        [Display(Name = "Purpose of Visit")]
        public string PurposeOfVisit { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Entry Time")]
        public DateTime EntryTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "Exit Time")]
        public DateTime? ExitTime { get; set; }

        [Required]
        public string RecordedByUserId { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Remarks { get; set; }
    }
}
