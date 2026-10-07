namespace CardNotificationEngine.Infrastructure.Notifications;

using CardNotificationEngine.Domain.Interfaces;

public class ConsoleNotifier : INotifier
{
    public async Task NotifyAsync(string customerId, string message, int delayMs, CancellationToken cancellationToken)
    {
        await Task.Delay(delayMs, cancellationToken);
        Console.WriteLine($"[BİLDİRİM] {customerId}: {message}");
    }
}