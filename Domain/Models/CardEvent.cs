namespace CardNotificationEngine.Domain.Models;

public class CardEvent
{
    public required string EventId { get; set; }
    public required string CustomerId { get; set; }
    public required int AmountMinor { get; set; }
    public required string ResultCode { get; set; }
    public string? Status { get; set; }
    public string? Notification { get; set; }
    public required string CreatedAtUtc { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EventId))
            throw new ArgumentException("EventId cannot be empty.");
        
        if (string.IsNullOrWhiteSpace(CustomerId))
            throw new ArgumentException("CustomerId cannot be empty.");
        
        if (AmountMinor <= 0)
            throw new ArgumentException("AmountMinor must be greater than 0.");
        
        if (string.IsNullOrWhiteSpace(ResultCode))
            throw new ArgumentException("ResultCode cannot be empty.");
        
        if (!IsValidResultCode(ResultCode))
            throw new ArgumentException($"Unknown ResultCode: {ResultCode}");
    }

    private static bool IsValidResultCode(string code) =>
        code is "APPROVED" or "LIMIT_DECLINED";
}