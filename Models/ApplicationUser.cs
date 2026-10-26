using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HostelEase.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Display(Name = "User Role")]
        public string UserType { get; set; } = "Student"; // "Admin", "Warden", "Student"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? ProfilePictureUrl { get; set; }

        // Navigation
        public virtual Student? Student { get; set; }
        public virtual Staff? Staff { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
