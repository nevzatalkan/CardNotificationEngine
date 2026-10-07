namespace CardNotificationEngine.Tests;

using Xunit;
using CardNotificationEngine.Infrastructure.Persistence;
using CardNotificationEngine.Domain.Models;
using System.IO;

public class SqliteEventStoreTests
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public SqliteEventStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "test.db");
    }

    [Fact]
    public async Task InsertEvent_ValidEvent_ReturnsTrue()
    {
        // Arrange
        var store = new SqliteEventStore(_dbPath);
        await store.InitializeAsync(CancellationToken.None);

        var cardEvent = new CardEvent
        {
            EventId = Guid.NewGuid().ToString(),
            CustomerId = "cust001",
            AmountMinor = 15000,
            ResultCode = "LIMIT_DECLINED",
            Status = "NEW",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        // Act
        var inserted = await store.InsertEventAsync(cardEvent, CancellationToken.None);

        // Assert
        Assert.True(inserted);

        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task InsertEvent_Duplicate_ReturnsFalse()
    {
        // Arrange
        var store = new SqliteEventStore(_dbPath);
        await store.InitializeAsync(CancellationToken.None);

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

        // Act
        var inserted1 = await store.InsertEventAsync(cardEvent, CancellationToken.None);
        var inserted2 = await store.InsertEventAsync(cardEvent, CancellationToken.None);

        // Assert
        Assert.True(inserted1);
        Assert.False(inserted2);

        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task UpdateEventStatus_ValidEvent_ReturnsOne()
    {
        // Arrange
        var store = new SqliteEventStore(_dbPath);
        await store.InitializeAsync(CancellationToken.None);

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

        await store.InsertEventAsync(cardEvent, CancellationToken.None);

        // Act
        var updated = await store.UpdateEventStatusAsync(eventId, "DONE", "Test notification", CancellationToken.None);

        // Assert
        Assert.Equal(1, updated);

        var retrieved = await store.GetEventAsync(eventId, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal("DONE", retrieved.Status);
        Assert.Equal("Test notification", retrieved.Notification);

        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GetPendingEventIds_ReturnsPendingEvents()
    {
        // Arrange
        var store = new SqliteEventStore(_dbPath);
        await store.InitializeAsync(CancellationToken.None);

        var eventId1 = Guid.NewGuid().ToString();
        var eventId2 = Guid.NewGuid().ToString();

        var cardEvent1 = new CardEvent
        {
            EventId = eventId1,
            CustomerId = "cust001",
            AmountMinor = 15000,
            ResultCode = "LIMIT_DECLINED",
            Status = "NEW",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        var cardEvent2 = new CardEvent
        {
            EventId = eventId2,
            CustomerId = "cust002",
            AmountMinor = 20000,
            ResultCode = "APPROVED",
            Status = "NEW",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        await store.InsertEventAsync(cardEvent1, CancellationToken.None);
        await store.InsertEventAsync(cardEvent2, CancellationToken.None);

        // Act
        var pendingIds = await store.GetPendingEventIdsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, pendingIds.Count);
        Assert.Contains(eventId1, pendingIds);
        Assert.Contains(eventId2, pendingIds);

        Directory.Delete(_tempDir, true);
    }
}