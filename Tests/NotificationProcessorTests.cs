namespace CardNotificationEngine.Tests;

using Xunit;
using CardNotificationEngine.Infrastructure.Persistence;
using CardNotificationEngine.Infrastructure.Messaging;
using CardNotificationEngine.Infrastructure.Notifications;
using CardNotificationEngine.Application.Services;
using CardNotificationEngine.Application.Rules;
using CardNotificationEngine.Domain.Models;
using System.IO;

public class NotificationProcessorTests
{
    private readonly string _tempDir;
    private readonly string _dbPath;
    private readonly string _rulesPath;

    public NotificationProcessorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "test.db");
        _rulesPath = Path.Combine(_tempDir, "rules.json");
    }

    private async Task SetupRulesFile(string rulesJson)
    {
        await File.WriteAllTextAsync(_rulesPath, rulesJson);
    }

    [Fact]
    public async Task ProcessEvent_MatchingRule_UpdatesStatusAndNotifies()
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

        await SetupRulesFile(rulesJson);

        var eventStore = new SqliteEventStore(_dbPath);
        await eventStore.InitializeAsync(CancellationToken.None);

        var eventQueue = new MemoryEventQueue();
        var notifier = new ConsoleNotifier();
        var ruleEngine = new RuleEngine(_rulesPath);
        await ruleEngine.InitializeAsync(CancellationToken.None);

        var ruleEvaluator = new RuleEvaluator(ruleEngine);
        var processor = new NotificationProcessor(eventStore, eventQueue, notifier, ruleEvaluator);

        var eventId = Guid.NewGuid().ToString();
        var cardEvent = new CardEvent
        {
            EventId = eventId,
            CustomerId = "cust001",
            AmountMinor = 15000,
            ResultCode = "LIMIT_DECLINED",
            Status = "NEW",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        await eventStore.InsertEventAsync(cardEvent, CancellationToken.None);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var processorTask = Task.Run(async () =>
        {
            await processor.StartProcessingAsync(cts.Token);
        });

        await eventQueue.EnqueueAsync(eventId, CancellationToken.None);
        eventQueue.CompleteWriter();

        try
        {
            await processorTask;
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        var updatedEvent = await eventStore.GetEventAsync(eventId, CancellationToken.None);
        Assert.NotNull(updatedEvent);
        Assert.Equal("DONE", updatedEvent.Status);
        Assert.NotNull(updatedEvent.Notification);
        Assert.Contains("Limitiniz yetersiz kaldi.", updatedEvent.Notification);

        ruleEngine.Dispose();
        Directory.Delete(_tempDir, true);
    }
}