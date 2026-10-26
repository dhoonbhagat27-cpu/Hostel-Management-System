using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using HostelEase.Models;

namespace HostelEase.ViewModels
{
    // Student ViewModels
    public class StudentListFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Department { get; set; }
        public string? AcademicYear { get; set; }
        public string? Status { get; set; } // "All", "Active", "Inactive"
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public List<Student> Students { get; set; } = new();
    }

    public class StudentCreateViewModel
    {
        [Display(Name = "Student ID / Enrollment No. (Leave blank to auto-generate)")]
        public string? StudentId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Student Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a department")]
        [Display(Name = "Course / Department")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an academic year")]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Resident Gender")]
        public string Gender { get; set; } = "Female";

        [Required(ErrorMessage = "Parent / Primary Guardian Name is required")]
        [Display(Name = "Parent / Guardian Name")]
        public string GuardianName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Guardian Phone is required")]
        [Phone]
        [Display(Name = "Guardian Contact Number")]
        public string GuardianContact { get; set; } = string.Empty;

        [Display(Name = "Local Guardian (LG) Name")]
        public string? LocalGuardianName { get; set; }

        [Phone]
        [Display(Name = "Local Guardian Contact")]
        public string? LocalGuardianContact { get; set; }

        [Display(Name = "Local Guardian Address & Relationship")]
        public string? LocalGuardianAddress { get; set; }

        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }

        [Phone]
        [Display(Name = "Emergency Contact Number")]
        public string? EmergencyContact { get; set; }

        [Required(ErrorMessage = "Permanent Address is required")]
        [Display(Name = "Permanent Home Address")]
        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Initial Password (Optional - defaults to Student@123)")]
        public string? Password { get; set; }

        [Display(Name = "Direct Bed Allocation (Optional)")]
        public int? SelectedBedId { get; set; }

        public List<SelectListItem> AvailableBedsList { get; set; } = new();
    }

    public class StudentEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;

        [Required]
        public string AcademicYear { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = "Male";

        [Required]
        public string GuardianName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string GuardianContact { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    // Staff ViewModels
    public class StaffCreateViewModel
    {
        [Required]
        [Display(Name = "Staff ID")]
        public string StaffId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Designation { get; set; } = "Warden"; // "Chief Warden", "Assistant Warden", "Hostel Admin"

        [Display(Name = "Assigned Hostel")]
        public int? AssignedHostelId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = "Warden@123";
    }

    // Room Allocation ViewModels
    public class AllocateBedViewModel
    {
        [Required(ErrorMessage = "Please select a student")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Please select a bed")]
        [Display(Name = "Bed to Allocate")]
        public int BedId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Allocation Date")]
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        [Display(Name = "Expected Vacating Date")]
        public DateTime? ExpectedVacatingDate { get; set; }

        [MaxLength(300)]
        public string? Remarks { get; set; }

        // Select lists
        public SelectList? StudentsList { get; set; }
        public SelectList? AvailableBedsList { get; set; }
    }

    public class TransferBedViewModel
    {
        public int CurrentAllocationId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdString { get; set; } = string.Empty;
        public string CurrentBedNumber { get; set; } = string.Empty;
        public string CurrentRoomNumber { get; set; } = string.Empty;
        public string CurrentHostelName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select target bed")]
        [Display(Name = "New Bed Assignment")]
        public int NewBedId { get; set; }

        [MaxLength(300)]
        [Display(Name = "Reason / Transfer Remarks")]
        public string? Remarks { get; set; }

        public SelectList? AvailableBedsList { get; set; }
    }

    public class VacateBedViewModel
    {
        public int AllocationId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string BedNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string HostelName { get; set; } = string.Empty;
        public DateTime AllocationDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Vacating Date")]
        public DateTime ActualVacatingDate { get; set; } = DateTime.UtcNow;

        [MaxLength(300)]
        [Display(Name = "Vacating Clearance Remarks")]
        public string? Remarks { get; set; }
    }

    // Fee ViewModels
    public class AssignFeeViewModel
    {
        [Required]
        [Display(Name = "Select Student")]
        public int StudentId { get; set; }

        [Required]
        [Display(Name = "Select Fee Structure")]
        public int FeeStructureId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public SelectList? StudentsList { get; set; }
        public SelectList? FeeStructuresList { get; set; }
    }

    public class RecordPaymentViewModel
    {
        public int StudentFeeId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdString { get; set; } = string.Empty;
        public string FeeTitle { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AlreadyPaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }

        [Required(ErrorMessage = "Payment amount is required")]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than zero")]
        [Display(Name = "Payment Amount (₹)")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "UPI"; // Cash, UPI, Bank Transfer, Demand Draft, Card

        [Required(ErrorMessage = "Transaction reference or receipt code is required")]
        [Display(Name = "Transaction Reference / UTR / Cheque No.")]
        public string TransactionReference { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Remarks { get; set; }
    }

    // Complaint ViewModels
    public class ComplaintCreateViewModel
    {
        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = "Electrical";

        [Required]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
    }

    public class ComplaintResolveViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string RoomInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }

        [Required]
        [Display(Name = "Update Status")]
        public string Status { get; set; } = "In Progress"; // In Progress, Resolved, Closed

        [Required(ErrorMessage = "Resolution remarks are required")]
        [StringLength(1000)]
        [Display(Name = "Resolution / Action Remarks")]
        public string ResolutionRemarks { get; set; } = string.Empty;

        [Display(Name = "Assigned Staff Member")]
        public int? AssignedStaffId { get; set; }

        public SelectList? StaffList { get; set; }
    }

    // Leave ViewModels
    public class LeaveApplyViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date (Departure)")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date (Expected Return)")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(3);

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(300)]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination city or address is required")]
        [StringLength(200)]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emergency contact number is required")]
        [Phone]
        [Display(Name = "Emergency Contact Number")]
        public string EmergencyContact { get; set; } = string.Empty;

        [Display(Name = "Parent/Guardian Informed?")]
        public bool ParentInformed { get; set; } = true;
    }

    public class LeaveReviewViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdString { get; set; } = string.Empty;
        public string RoomDetails { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public bool ParentInformed { get; set; }

        [Required]
        [Display(Name = "Decision")]
        public string Status { get; set; } = "Approved"; // "Approved", "Rejected"

        [Required(ErrorMessage = "Remarks are required for review decisions")]
        [StringLength(500)]
        [Display(Name = "Warden / Admin Remarks")]
        public string ReviewRemarks { get; set; } = string.Empty;
    }

    // Visitor ViewModels
    public class VisitorRegisterViewModel
    {
        [Required(ErrorMessage = "Please select the student being visited")]
        [Display(Name = "Student Visited")]
        public int StudentId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Visitor Name")]
        public string VisitorName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Visitor Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Relationship to Student")]
        public string RelationToStudent { get; set; } = "Parent";

        [Required]
        [StringLength(200)]
        [Display(Name = "Purpose of Visit")]
        public string PurposeOfVisit { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Remarks { get; set; }

        public SelectList? StudentsList { get; set; }
    }

    // Reports ViewModels
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "Occupancy"; // Occupancy, Fees, Complaints, Leaves, Visitors
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? HostelId { get; set; }
        public string? Status { get; set; }
    }
}
