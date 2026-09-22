namespace Lab789HRWebAPI.Core.Dtos
{
    public class CreateTransferRequestDto
    {
        public string ToDepartment { get; set; } = string.Empty;
        public string ToPosition { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewTransferRequestDto
    {
        public bool IsApproved { get; set; } // true = Approve, false = Reject
        public string? ReviewNote { get; set; }
        public string? ApprovedPosition { get; set; } // Approved position (if blank, defaults to request ToPosition)
    }
}