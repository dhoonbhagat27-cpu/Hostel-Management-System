using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelEase.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Student ID / Enrollment No.")]
        public string StudentId { get; set; } = string.Empty;

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
        [MaxLength(100)]
        public string Department { get; set; } = "Computer Engineering";

        [Required]
        [MaxLength(30)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = "4th Year";

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(15)]
        public string Gender { get; set; } = "Female";

        [Required]
        [MaxLength(100)]
        [Display(Name = "Guardian Name")]
        public string GuardianName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        [Display(Name = "Guardian Contact")]
        public string GuardianContact { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Local Guardian Name")]
        public string? LocalGuardianName { get; set; }

        [Phone]
        [MaxLength(20)]
        [Display(Name = "Local Guardian Contact")]
        public string? LocalGuardianContact { get; set; }

        [MaxLength(250)]
        [Display(Name = "Local Guardian Address")]
        public string? LocalGuardianAddress { get; set; }

        [MaxLength(10)]
        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }

        [Phone]
        [MaxLength(20)]
        [Display(Name = "Emergency Contact Number")]
        public string? EmergencyContact { get; set; }

        [Required]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Admission Date")]
        public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<RoomAllocation> Allocations { get; set; } = new List<RoomAllocation>();
        public virtual ICollection<StudentFee> Fees { get; set; } = new List<StudentFee>();
        public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public virtual ICollection<Visitor> Visitors { get; set; } = new List<Visitor>();
    }
}
