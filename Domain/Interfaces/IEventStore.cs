namespace CardNotificationEngine.Domain.Interfaces;

using CardNotificationEngine.Domain.Models;

public interface IEventStore
{
    Task<bool> InsertEventAsync(CardEvent cardEvent, CancellationToken cancellationToken);
    Task<CardEvent?> GetEventAsync(string eventId, CancellationToken cancellationToken);
    Task<int> UpdateEventStatusAsync(string eventId, string status, string? notification, CancellationToken cancellationToken);
    Task<List<string>> GetPendingEventIdsAsync(CancellationToken cancellationToken);
    Task InitializeAsync(CancellationToken cancellationToken);
}