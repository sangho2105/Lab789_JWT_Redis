namespace Lab789HRWebAPI.Core.Entities
{
    public class DepartmentTransferRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeEmail { get; set; } = string.Empty;
        public string FromDepartment { get; set; } = string.Empty;
        public string FromPosition { get; set; } = string.Empty;
        public string ToDepartment { get; set; } = string.Empty;
        public string ToPosition { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        // PENDING, APPROVED, REJECTED
        public string Status { get; set; } = "PENDING";

        public string? ReviewerEmail { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}