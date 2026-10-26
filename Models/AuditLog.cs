using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [MaxLength(100)]
        public string? UserEmail { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, Allocate, Vacate, Payment, Login, LeaveApproval, ComplaintStatus

        [Required]
        [MaxLength(50)]
        public string EntityName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EntityId { get; set; }

        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
