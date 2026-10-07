namespace CardNotificationEngine.Tests;

using Xunit;
using CardNotificationEngine.Application.Rules;
using CardNotificationEngine.Domain.Models;
using System.IO;

public class RuleEngineTests
{
    private readonly string _tempDir;
    private readonly string _rulesPath;

    public RuleEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _rulesPath = Path.Combine(_tempDir, "rules.json");
    }

    [Fact]
    public async Task EvaluateEvent_MatchesRule_ReturnsMessage()
    {
        // Arrange
        var rulesJson = """
            [
              {
                "id": "limit-offer",
                "enabled": true,
                "all": [
                  { "field": "ResultCode", "op": "eq", "value": "LIMIT_DECLINED" },
                  { "field": "AmountMinor", "op": "gt", "value": 10000 }
                ],
                "message": "Limitiniz yetersiz kaldi."
              }
            ]
            """;

        await File.WriteAllTextAsync(_rulesPath, rulesJson);

        var ruleEngine = new RuleEngine(_rulesPath);
        await ruleEngine.InitializeAsync(CancellationToken.None);

        var cardEvent = new CardEvent
        {
            EventId = Guid.NewGuid().ToString(),
            CustomerId = "cust001",
            AmountMinor = 15000,
            ResultCode = "LIMIT_DECLINED",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        var ruleEvaluator = new RuleEvaluator(ruleEngine);

        // Act
        var messages = ruleEvaluator.EvaluateEvent(cardEvent);

        // Assert
        Assert.Single(messages);
        Assert.Equal("Limitiniz yetersiz kaldi.", messages[0]);

        ruleEngine.Dispose();
        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task EvaluateEvent_DisabledRule_ReturnsEmpty()
    {
        // Arrange
        var rulesJson = """
            [
              {
                "id": "limit-offer",
                "enabled": false,
                "all": [
                  { "field": "ResultCode", "op": "eq", "value": "LIMIT_DECLINED" },
                  { "field": "AmountMinor", "op": "gt", "value": 10000 }
                ],
                "message": "Limitiniz yetersiz kaldi."
              }
            ]
            """;

        await File.WriteAllTextAsync(_rulesPath, rulesJson);

        var ruleEngine = new RuleEngine(_rulesPath);
        await ruleEngine.InitializeAsync(CancellationToken.None);

        var cardEvent = new CardEvent
        {
            EventId = Guid.NewGuid().ToString(),
            CustomerId = "cust001",
            AmountMinor = 15000,
            ResultCode = "LIMIT_DECLINED",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        var ruleEvaluator = new RuleEvaluator(ruleEngine);

        // Act
        var messages = ruleEvaluator.EvaluateEvent(cardEvent);

        // Assert
        Assert.Empty(messages);

        ruleEngine.Dispose();
        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task EvaluateEvent_NoMatch_ReturnsEmpty()
    {
        // Arrange
        var rulesJson = """
            [
              {
                "id": "limit-offer",
                "enabled": true,
                "all": [
                  { "field": "ResultCode", "op": "eq", "value": "LIMIT_DECLINED" },
                  { "field": "AmountMinor", "op": "gt", "value": 50000 }
                ],
                "message": "Limitiniz yetersiz kaldi."
              }
            ]
            """;

        await File.WriteAllTextAsync(_rulesPath, rulesJson);

        var ruleEngine = new RuleEngine(_rulesPath);
        await ruleEngine.InitializeAsync(CancellationToken.None);

        var cardEvent = new CardEvent
        {
            EventId = Guid.NewGuid().ToString(),
            CustomerId = "cust001",
            AmountMinor = 15000,
            ResultCode = "LIMIT_DECLINED",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        var ruleEvaluator = new RuleEvaluator(ruleEngine);

        // Act
        var messages = ruleEvaluator.EvaluateEvent(cardEvent);

        // Assert
        Assert.Empty(messages);

        ruleEngine.Dispose();
        Directory.Delete(_tempDir, true);
    }
}