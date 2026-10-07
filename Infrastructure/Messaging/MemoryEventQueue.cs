namespace CardNotificationEngine.Infrastructure.Messaging;

using System.Threading.Channels;
using CardNotificationEngine.Domain.Interfaces;

public class MemoryEventQueue : IEventQueue
{
    private readonly Channel<string> _channel;

    public MemoryEventQueue(int capacity = 1000)
    {
        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ValueTask EnqueueAsync(string eventId, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(eventId, cancellationToken);
    }

    public async IAsyncEnumerable<string> DequeueAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var eventId in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return eventId;
        }
    }

    public void CompleteWriter()
    {
        _channel.Writer.TryComplete();
    }
}