using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Display(Name = "Target Audience")]
        public string TargetAudience { get; set; } = "All"; // "All", "Students", "Wardens", "Hostel Specific"

        [Display(Name = "Specific Hostel")]
        public int? HostelId { get; set; }

        [ForeignKey("HostelId")]
        public virtual Hostel? Hostel { get; set; }

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // "Normal", "Important", "Urgent"

        [Display(Name = "Publish Date")]
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [Required]
        public string PublishedByUserId { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
