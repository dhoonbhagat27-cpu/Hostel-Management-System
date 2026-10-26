using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Hostel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Hostel Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Display(Name = "Hostel Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Display(Name = "Hostel Type")]
        public string HostelType { get; set; } = "Boys"; // "Boys", "Girls", "Co-ed"

        [Required]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Assigned Warden")]
        public int? AssignedWardenId { get; set; }

        [ForeignKey("AssignedWardenId")]
        public virtual Staff? AssignedWarden { get; set; }

        [Range(1, 20)]
        [Display(Name = "Total Floors")]
        public int TotalFloors { get; set; } = 3;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // "Active", "Maintenance", "Inactive"

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
        public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    }
}
