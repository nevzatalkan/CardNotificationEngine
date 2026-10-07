namespace CardNotificationEngine.Application.Services;

using CardNotificationEngine.Application.Rules;
using CardNotificationEngine.Domain.Interfaces;

public class NotificationProcessor
{
    private readonly IEventStore _eventStore;
    private readonly IEventQueue _eventQueue;
    private readonly INotifier _notifier;
    private readonly RuleEvaluator _ruleEvaluator;

    public NotificationProcessor(
        IEventStore eventStore,
        IEventQueue eventQueue,
        INotifier notifier,
        RuleEvaluator ruleEvaluator)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _ruleEvaluator = ruleEvaluator ?? throw new ArgumentNullException(nameof(ruleEvaluator));
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        await foreach (var eventId in _eventQueue.DequeueAsync(cancellationToken))
        {
            await ProcessEventAsync(eventId, cancellationToken);
        }
    }

    private async Task ProcessEventAsync(string eventId, CancellationToken cancellationToken)
    {
        try
        {
            var cardEvent = await _eventStore.GetEventAsync(eventId, cancellationToken);
            if (cardEvent is null)
            {
                Console.WriteLine($"[WARN] Event not found: {eventId}");
                return;
            }

            var messages = _ruleEvaluator.EvaluateEvent(cardEvent);
            var notification = messages.Count > 0 ? string.Join(" | ", messages) : null;

            if (notification is not null)
            {
                await _notifier.NotifyAsync(cardEvent.CustomerId, notification, 50, cancellationToken);
            }
            else
            {
                Console.WriteLine($"[KARAR] No matching rules for event {eventId}");
            }

            await _eventStore.UpdateEventStatusAsync(eventId, "DONE", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to process event {eventId}: {ex.Message}");
        }
    }
}