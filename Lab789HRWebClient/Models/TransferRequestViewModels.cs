using System.ComponentModel.DataAnnotations;

namespace Lab789HRWebClient.Models
{
    public class DepartmentTransferRequestViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeEmail { get; set; } = string.Empty;
        public string FromDepartment { get; set; } = string.Empty;
        public string FromPosition { get; set; } = string.Empty;
        public string ToDepartment { get; set; } = string.Empty;
        public string ToPosition { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
        public string? ReviewerEmail { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class CreateTransferRequestViewModel
    {
        // Current employee information for display
        [Display(Name = "Full Name")]
        public string CurrentFullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string CurrentEmail { get; set; } = string.Empty;

        [Display(Name = "Current Department")]
        public string CurrentDepartment { get; set; } = string.Empty;

        [Display(Name = "Current Position")]
        public string CurrentPosition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select or specify destination department")]
        [Display(Name = "Target Department")]
        public string ToDepartment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select or specify target position")]
        [Display(Name = "Target Position")]
        public string ToPosition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter reason for transfer")]
        [Display(Name = "Transfer Reason")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewTransferRequestViewModel
    {
        public int RequestId { get; set; }
        public bool IsApproved { get; set; }
        public string? ReviewNote { get; set; }
        public string? ApprovedPosition { get; set; }
    }
}
