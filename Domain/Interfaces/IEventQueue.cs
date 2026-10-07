namespace CardNotificationEngine.Domain.Interfaces;

public interface IEventQueue
{
    ValueTask EnqueueAsync(string eventId, CancellationToken cancellationToken);
    IAsyncEnumerable<string> DequeueAsync(CancellationToken cancellationToken);
}