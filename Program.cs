using System.Globalization;
using CardNotificationEngine.Application.Rules;
using CardNotificationEngine.Application.Services;
using CardNotificationEngine.Infrastructure.Messaging;
using CardNotificationEngine.Infrastructure.Notifications;
using CardNotificationEngine.Infrastructure.Persistence;
using CardNotificationEngine.Domain.Models;
using CardNotificationEngine.Domain.Interfaces;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

const string dbPath = "cards.db";
const string rulesPath = "rules.json";

// Infrastructure
var eventStore = new SqliteEventStore(dbPath);
var eventQueue = new MemoryEventQueue();
var notifier = new ConsoleNotifier();
var ruleEngine = new RuleEngine(rulesPath);

// Application
var ruleEvaluator = new RuleEvaluator(ruleEngine);
var processor = new NotificationProcessor(eventStore, eventQueue, notifier, ruleEvaluator);

// Initialize
await eventStore.InitializeAsync(cts.Token);
await ruleEngine.InitializeAsync(cts.Token);

// Recover pending events
var pendingIds = await eventStore.GetPendingEventIdsAsync(cts.Token);
foreach (var id in pendingIds)
{
    await eventQueue.EnqueueAsync(id, cts.Token);
}

// Start processing
var processorTask = processor.StartProcessingAsync(cts.Token);

Console.WriteLine("[INFO] Card Notification Engine started. Type 'send <customerId> <amountMinor> <resultCode> [eventId]' or 'exit'");

// Command loop
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[INFO] Shutting down...");
            cts.Cancel();
            break;
        }

        var parts = input.Split(' ');
        if (parts.Length < 4)
        {
            Console.WriteLine("[ERROR] Usage: send <customerId> <amountMinor> <resultCode> [eventId]");
            continue;
        }

        var command = parts[0];
        if (!command.Equals("send", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[ERROR] Unknown command. Use 'send' or 'exit'");
            continue;
        }

        var customerId = parts[1];
        var amountStr = parts[2];
        var resultCode = parts[3];
        var eventId = parts.Length > 4 ? parts[4] : Guid.NewGuid().ToString();

        if (!int.TryParse(amountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
        {
            Console.WriteLine("[ERROR] AmountMinor must be a valid integer.");
            continue;
        }

        var cardEvent = new CardEvent
        {
            EventId = eventId,
            CustomerId = customerId,
            AmountMinor = amount,
            ResultCode = resultCode,
            Status = "NEW",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        try
        {
            cardEvent.Validate();
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[ERROR] Validation failed: {ex.Message}");
            continue;
        }

        var inserted = await eventStore.InsertEventAsync(cardEvent, cts.Token);
        if (!inserted)
        {
            Console.WriteLine($"[WARN] Duplicate EventId: {eventId}");
            continue;
        }

        Console.WriteLine($"[OK] Event created: {eventId}");
        await eventQueue.EnqueueAsync(eventId, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
    }
}

// Graceful shutdown
eventQueue.CompleteWriter();

try
{
    await processorTask;
}
catch (OperationCanceledException)
{
    // Expected
}

ruleEngine.Dispose();
Console.WriteLine("[INFO] Shutdown complete.");