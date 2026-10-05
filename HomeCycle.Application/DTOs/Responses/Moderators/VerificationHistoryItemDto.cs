namespace HomeCycle.Application.DTOs.Responses.Moderators
{
    public class VerificationHistoryItemDto
    {
        public Guid ProfileId { get; set; }
        public string? DisplayName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectReason { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public Guid? VerifiedBy { get; set; }
    }
}
