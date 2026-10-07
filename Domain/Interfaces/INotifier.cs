namespace CardNotificationEngine.Domain.Interfaces;

public interface INotifier
{
    Task NotifyAsync(string customerId, string message, int delayMs, CancellationToken cancellationToken);
}