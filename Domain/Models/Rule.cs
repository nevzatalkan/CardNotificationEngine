namespace CardNotificationEngine.Domain.Models;

public class Rule
{
    public required string Id { get; set; }
    public required bool Enabled { get; set; }
    public required List<Condition> All { get; set; }
    public required string Message { get; set; }
}

public class Condition
{
    public required string Field { get; set; }
    public required string Op { get; set; }
    public required object Value { get; set; }

    public void Validate()
    {
        if (Field is not ("ResultCode" or "AmountMinor"))
            throw new ArgumentException($"Field '{Field}' not in whitelist.");
        
        if (Op is not ("eq" or "gt"))
            throw new ArgumentException($"Operator '{Op}' not supported.");
    }
}