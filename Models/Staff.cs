using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Staff
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Staff ID")]
        public string StaffId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Designation { get; set; } = "Warden"; // "Chief Warden", "Assistant Warden", "Caregiver", "Admin"

        [Display(Name = "Assigned Hostel")]
        public int? AssignedHostelId { get; set; }

        [ForeignKey("AssignedHostelId")]
        public virtual Hostel? AssignedHostel { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
    }
}
