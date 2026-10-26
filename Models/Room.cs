using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Hostel")]
        public int HostelId { get; set; }

        [ForeignKey("HostelId")]
        public virtual Hostel? Hostel { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Range(0, 20)]
        public int Floor { get; set; } = 1;

        [Required]
        [MaxLength(30)]
        [Display(Name = "Room Type")]
        public string RoomType { get; set; } = "Double"; // "Single", "Double", "Triple", "Four-Sharing"

        [Range(1, 10)]
        [Display(Name = "Capacity (Beds)")]
        public int Capacity { get; set; } = 2;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Monthly Fee")]
        public decimal MonthlyFee { get; set; } = 5000.00m;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available"; // "Available", "Full", "Maintenance", "Inactive"

        [MaxLength(300)]
        public string? Description { get; set; }

        // Navigation
        public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
    }
}
