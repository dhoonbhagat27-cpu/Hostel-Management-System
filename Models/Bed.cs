using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Bed
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Bed Identifier")]
        public string BedNumber { get; set; } = string.Empty; // e.g. "101-A", "101-B"

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available"; // "Available", "Occupied", "Maintenance", "Reserved"

        [ConcurrencyCheck]
        public Guid RowVersion { get; set; } = Guid.NewGuid();

        // Navigation
        public virtual ICollection<RoomAllocation> Allocations { get; set; } = new List<RoomAllocation>();
    }
}
