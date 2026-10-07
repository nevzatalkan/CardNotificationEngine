namespace CardNotificationEngine.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;
using CardNotificationEngine.Domain.Interfaces;
using CardNotificationEngine.Domain.Models;

public class SqliteEventStore : IEventStore
{
    private readonly string _connectionString;

    public SqliteEventStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};Cache=Shared";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS CardEvents (
                EventId TEXT PRIMARY KEY,
                CustomerId TEXT NOT NULL,
                AmountMinor INTEGER NOT NULL,
                ResultCode TEXT NOT NULL,
                Status TEXT,
                Notification TEXT,
                CreatedAtUtc TEXT NOT NULL
            );
        ";

        await command.ExecuteNonQueryAsync(cancellationToken);
        await connection.CloseAsync();
    }

    public async Task<bool> InsertEventAsync(CardEvent cardEvent, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO CardEvents 
            (EventId, CustomerId, AmountMinor, ResultCode, Status, Notification, CreatedAtUtc)
            VALUES (@EventId, @CustomerId, @AmountMinor, @ResultCode, @Status, @Notification, @CreatedAtUtc);
        ";

        command.Parameters.AddWithValue("@EventId", cardEvent.EventId);
        command.Parameters.AddWithValue("@CustomerId", cardEvent.CustomerId);
        command.Parameters.AddWithValue("@AmountMinor", cardEvent.AmountMinor);
        command.Parameters.AddWithValue("@ResultCode", cardEvent.ResultCode);
        command.Parameters.AddWithValue("@Status", cardEvent.Status ?? "NEW");
        command.Parameters.AddWithValue("@Notification", cardEvent.Notification ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAtUtc", cardEvent.CreatedAtUtc);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        await connection.CloseAsync();

        return rowsAffected > 0;
    }

    public async Task<CardEvent?> GetEventAsync(string eventId, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT EventId, CustomerId, AmountMinor, ResultCode, Status, Notification, CreatedAtUtc FROM CardEvents WHERE EventId = @EventId;";
        command.Parameters.AddWithValue("@EventId", eventId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new CardEvent
            {
                EventId = reader.GetString(0),
                CustomerId = reader.GetString(1),
                AmountMinor = reader.GetInt32(2),
                ResultCode = reader.GetString(3),
                Status = reader.IsDBNull(4) ? null : reader.GetString(4),
                Notification = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAtUtc = reader.GetString(6)
            };
        }

        await connection.CloseAsync();
        return null;
    }

    public async Task<int> UpdateEventStatusAsync(string eventId, string status, string? notification, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE CardEvents 
            SET Status = @Status, Notification = @Notification 
            WHERE EventId = @EventId AND Status = 'NEW';
        ";

        command.Parameters.AddWithValue("@EventId", eventId);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@Notification", notification ?? (object)DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        await connection.CloseAsync();

        return rowsAffected;
    }

    public async Task<List<string>> GetPendingEventIdsAsync(CancellationToken cancellationToken)
    {
        var pendingIds = new List<string>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT EventId FROM CardEvents WHERE Status = 'NEW';";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            pendingIds.Add(reader.GetString(0));
        }

        await connection.CloseAsync();
        return pendingIds;
    }
}