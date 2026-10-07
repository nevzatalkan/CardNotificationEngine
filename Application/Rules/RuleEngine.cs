namespace CardNotificationEngine.Application.Rules;

using System.Text.Json;
using CardNotificationEngine.Domain.Models;

public class RuleEngine
{
    private readonly string _rulesPath;
    private List<Rule> _rules = new();
    private FileSystemWatcher? _watcher;

    public RuleEngine(string rulesPath)
    {
        _rulesPath = rulesPath ?? throw new ArgumentNullException(nameof(rulesPath));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await LoadRulesAsync(cancellationToken);
        SetupWatcher();
    }

    private async Task LoadRulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_rulesPath))
            {
                Console.WriteLine($"[WARN] Rules file not found: {_rulesPath}");
                return;
            }

            var json = await File.ReadAllTextAsync(_rulesPath, cancellationToken);
            var rulesArray = JsonSerializer.Deserialize<Rule[]>(json) ?? Array.Empty<Rule>();

            foreach (var rule in rulesArray)
            {
                foreach (var condition in rule.All)
                {
                    condition.Validate();
                }
            }

            _rules = rulesArray.ToList();
            Console.WriteLine($"[INFO] Loaded {_rules.Count} rules.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to load rules: {ex.Message}. Keeping previous rules.");
        }
    }

    private void SetupWatcher()
    {
        var directory = Path.GetDirectoryName(_rulesPath);
        var fileName = Path.GetFileName(_rulesPath);

        _watcher = new FileSystemWatcher(directory ?? ".", fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite
        };

        _watcher.Changed += async (s, e) =>
        {
            await Task.Delay(100); // Avoid rapid reloads
            await LoadRulesAsync(CancellationToken.None);
        };

        _watcher.EnableRaisingEvents = true;
    }

    public List<Rule> GetApplicableRules(CardEvent cardEvent)
    {
        return _rules
            .Where(r => r.Enabled && EvaluateConditions(r.All, cardEvent))
            .ToList();
    }

    private bool EvaluateConditions(List<Condition> conditions, CardEvent cardEvent)
    {
        return conditions.All(c => EvaluateCondition(c, cardEvent));
    }

    private bool EvaluateCondition(Condition condition, CardEvent cardEvent)
    {
        var fieldValue = condition.Field switch
        {
            "ResultCode" => (object?)cardEvent.ResultCode,
            "AmountMinor" => cardEvent.AmountMinor,
            _ => null
        };

        if (fieldValue is null)
            return false;

        return condition.Op switch
        {
            "eq" => Equals(fieldValue, Convert.ChangeType(condition.Value, fieldValue.GetType())),
            "gt" => CompareGreaterThan(fieldValue, condition.Value),
            _ => false
        };
    }

    private bool CompareGreaterThan(object fieldValue, object conditionValue)
    {
        if (fieldValue is int intField && conditionValue is JsonElement jsonElement)
        {
            if (int.TryParse(jsonElement.ToString(), out var intCondition))
                return intField > intCondition;
        }

        if (fieldValue is int intField2 && conditionValue is int intCondition2)
            return intField2 > intCondition2;

        return false;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}