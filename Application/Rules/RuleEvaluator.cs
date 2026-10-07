namespace CardNotificationEngine.Application.Rules;

using CardNotificationEngine.Domain.Models;

public class RuleEvaluator
{
    private readonly RuleEngine _ruleEngine;

    public RuleEvaluator(RuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
    }

    public List<string> EvaluateEvent(CardEvent cardEvent)
    {
        var applicableRules = _ruleEngine.GetApplicableRules(cardEvent);
        return applicableRules.Select(r => r.Message).ToList();
    }
}