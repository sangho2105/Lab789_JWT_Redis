using System.ComponentModel.DataAnnotations;

namespace Lab789HRWebAPI.Core.Dtos
{
    public class EmployeeDto
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
    }
}
