using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;


public class IntentScorer
{
    private readonly List<IntentRule> _rules;

    public IntentScorer()
    {
        _rules = new List<IntentRule>
        {
            new IntentRule
            {
                Label = "Support",
                Keywords = new() { "error", "issue", "bug", "failed", "not working" },
                Weight = 20
            },
            new IntentRule
            {
                Label = "Sales",
                Keywords = new() { "quotation", "pricing", "proposal", "purchase" },
                Weight = 20
            },
            new IntentRule
            {
                Label = "General Query",
                Keywords = new() { "Hey", "Hi", "Hello", "Dear" },
                Weight = 20
            },
            new IntentRule
            {
                Label = "Finance",
                Keywords = new() { "invoice", "payment", "transaction" },
                Weight = 20
            }
        };
    }


    public (string label, int score) Calculate(string subject, string body)
    {
        var scores = new Dictionary<string, int>();
        var content = (subject + " " + body).ToLower();

        foreach (var rule in _rules)
        {
            foreach (var keyword in rule.Keywords)
            {
                if (content.Contains(keyword.ToLower()))
                {
                    if (!scores.ContainsKey(rule.Label))
                        scores[rule.Label] = 0;

                    scores[rule.Label] += rule.Weight;
                }
            }
        }

        if (!scores.Any())
            return ("Uncategorized", 0);

        var best = scores.OrderByDescending(x => x.Value).First();

        return (best.Key, best.Value);

    }
}