# Card Notification Engine

Minimal .NET kart işlemi → kural değerlendirme → bildirim akışını gösteren prototip.

## Özellikler

- ✅ Tek konsol projesi (net8.0)
- ✅ SQLite veri depolama (microsoft.data.sqlite)
- ✅ JSON-based kural motoru (FileSystemWatcher ile dinamik yükleme)
- ✅ Async/await ve CancellationToken
- ✅ Interface-based (IEventStore, IEventQueue, INotifier)
- ✅ Parametreli SQL (injection koruması)
- ✅ xUnit testler

## Proje Yapısı

```
Domain/          → Models, Interfaces (hiçbir bağımlılık yok)
Application/     → RuleEngine, NotificationProcessor
Infrastructure/  → SqliteEventStore, MemoryEventQueue, ConsoleNotifier
Program.cs       → Entry point
```

## Kurulum

```bash
dotnet build
```

## Çalıştırma

```bash
dotnet run
```

Komut: `send <customerId> <amountMinor> <resultCode> [eventId]`

Örnekler:
```
send cust001 15000 LIMIT_DECLINED
send cust002 5000 LIMIT_DECLINED custom-event-123
exit
```

## Testler

```bash
dotnet test
```

## Kural Yapısı (rules.json)

```json
{
  "id": "limit-offer",
  "enabled": true,
  "all": [
    {"field": "ResultCode", "op": "eq", "value": "LIMIT_DECLINED"},
    {"field": "AmountMinor", "op": "gt", "value": 10000}
  ],
  "message": "Limitiniz yetersiz kaldi."
}
```

### Desteklenen Operatörler
- `eq`: Eşit
- `gt`: Büyüktür

### Desteklenen Alanlar (Whitelist)
- `ResultCode`
- `AmountMinor`

## Akış

1. **Giriş**: Konsol komutu → `send customerId amountMinor resultCode`
2. **Validasyon**: Boş alan yok, amountMinor > 0, resultCode geçerli
3. **Kayıt**: SQLite'ye INSERT OR IGNORE (duplicate check)
4. **Kuyruk**: EventId → Channel<string>
5. **Processing**: Worker → Event oku → Kural değerlendir → Status='DONE' + Notification
6. **Bildirim**: [BİLDİRİM] customerId: mesaj

## Veri Tabanı

`cards.db` (SQLite):
```sql
CREATE TABLE CardEvents (
    EventId TEXT PRIMARY KEY,
    CustomerId TEXT NOT NULL,
    AmountMinor INTEGER NOT NULL,
    ResultCode TEXT NOT NULL,
    Status TEXT,
    Notification TEXT,
    CreatedAtUtc TEXT NOT NULL
);
```

## Genişletilebilirlik

Interface'ler sayesinde Azure SQL, Service Bus, SignalR ile değiştirilebilir:

- `IEventStore` → SqliteEventStore → Azure SQL
- `IEventQueue` → MemoryEventQueue → Service Bus
- `INotifier` → ConsoleNotifier → SignalR
