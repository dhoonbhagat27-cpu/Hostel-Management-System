using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HostelEase.ViewModels
{
    /// <summary>
    /// Custom validation attribute to enforce mandatory boolean agreement (e.g. Terms, Policies, Curfew Declarations).
    /// Works seamlessly on both Server-Side (C#) and Client-Side (jQuery Unobtrusive Validation via data-val-mustbetrue).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
    {
        public MustBeTrueAttribute() : base("You must accept the agreement to continue.") { }

        public override bool IsValid(object? value)
        {
            return value is bool b && b;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-mustbetrue", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        }

        private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key)) return false;
            attributes.Add(key, value);
            return true;
        }
    }
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student ID is required")]
        [StringLength(30, MinimumLength = 3)]
        [Display(Name = "Student ID / Enrollment Number")]
        public string StudentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Course / Department")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Academic Year is required")]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(?i)female$", ErrorMessage = "Only female candidates are eligible for admission into the Girls' Hostel.")]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = "Female";

        [Required(ErrorMessage = "Guardian Name is required")]
        [Display(Name = "Parent / Primary Guardian Name")]
        public string GuardianName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Guardian Contact is required")]
        [Phone]
        [Display(Name = "Parent / Primary Guardian Phone")]
        public string GuardianContact { get; set; } = string.Empty;

        [Display(Name = "Local Guardian (LG) Name")]
        public string? LocalGuardianName { get; set; }

        [Phone]
        [Display(Name = "Local Guardian Phone Number")]
        public string? LocalGuardianContact { get; set; }

        [Display(Name = "Local Guardian Address & Relationship")]
        public string? LocalGuardianAddress { get; set; }

        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; } = "B+";

        [Phone]
        [Display(Name = "Emergency Contact Number")]
        public string? EmergencyContact { get; set; }

        [Required(ErrorMessage = "Permanent Address is required")]
        [StringLength(250)]
        [Display(Name = "Permanent Home Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MustBeTrue(ErrorMessage = "You must accept the Girls' Hostel Regulations & 8:30 PM Night Curfew Policy.")]
        [Display(Name = "I acknowledge and agree to the Girls' Hostel Rules, 8:30 PM Evening Curfew, and Gate-Pass Security Protocol.")]
        public bool AgreeToPolicies { get; set; }

        /// <summary>
        /// Alternate alias property for AgreeToPolicies to support binding by either name.
        /// </summary>
        [Display(Name = "Accept Hostel Rules")]
        public bool AcceptHostelRules
        {
            get => AgreeToPolicies;
            set => AgreeToPolicies = value;
        }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Registered Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Student details if applicable
        public string? StudentId { get; set; }
        public string? Department { get; set; }
        public string? AcademicYear { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianContact { get; set; }
        public string? Address { get; set; }
        public string? AllocatedRoom { get; set; }
        public string? AllocatedBed { get; set; }

        // Staff details if applicable
        public string? StaffId { get; set; }
        public string? Designation { get; set; }
        public string? AssignedHostelName { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
