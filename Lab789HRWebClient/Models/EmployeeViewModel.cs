using System.ComponentModel.DataAnnotations;

namespace Lab789HRWebClient.Models
{
    public class EmployeeViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;

        [Range(1, 1000000)]
        public decimal Salary { get; set; }

        [Required(ErrorMessage = "Password is required for employee account")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "Employee@123";
    }
}
