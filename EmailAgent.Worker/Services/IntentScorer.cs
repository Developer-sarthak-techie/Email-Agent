using System.Text.RegularExpressions;
using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;

public class IntentScorer
{
    private readonly Dictionary<FintechEmailIntent.FintechEmailIntentEnum,
        (List<string> StrongPhrases,
        List<string> Keywords,
        int PhraseWeight,
        int KeywordWeight)> _rules;

    private readonly List<string> _urgencyWords = new()
    {
        "urgent", "immediately", "asap", "priority", "critical", "high priority"
    };

    public IntentScorer()
    {
        _rules = new()
        {
            {
                FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation,
                (
                    new()
                    {
                        "payment completed",
                        "transaction successful",
                        "invoice paid",
                        "fund transfer completed",
                        "amount credited successfully"
                    },
                    new()
                    {
                        "payment", "transaction", "credited", "utr", "reference",
                        "remittance", "neft", "rtgs", "imps", "transfer",
                        "payout", "settlement", "receipt", "txn", "paid"
                    },
                    40,
                    15
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue,
                (
                    new()
                    {
                        "transaction failed",
                        "amount deducted but not credited",
                        "duplicate debit",
                        "payment declined",
                        "pending transaction"
                    },
                    new()
                    {
                        "failed", "pending", "error", "declined", "deducted",
                        "not credited", "stuck", "timeout", "gateway",
                        "processing", "double charged", "incorrect debit",
                        "unsuccessful", "reversal not processed"
                    },
                    45,
                    18
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.FraudAlert,
                (
                    new()
                    {
                        "unauthorized transaction",
                        "account compromised",
                        "fraudulent activity",
                        "security breach"
                    },
                    new()
                    {
                        "fraud", "unauthorized", "suspicious",
                        "unknown debit", "phishing",
                        "identity theft", "card misuse",
                        "blocked card", "risk alert"
                    },
                    60,
                    25
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.RefundRequest,
                (
                    new()
                    {
                        "refund request",
                        "money back",
                        "reverse transaction",
                        "refund not received"
                    },
                    new()
                    {
                        "refund", "reversal", "cancel",
                        "return", "credit back",
                        "refund status", "refund pending"
                    },
                    40,
                    20
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.AccountAccessIssue,
                (
                    new()
                    {
                        "unable to login",
                        "account blocked",
                        "otp not received"
                    },
                    new()
                    {
                        "login", "blocked", "otp",
                        "password reset", "access denied",
                        "authentication failed", "mfa"
                    },
                    35,
                    15
                )
            }
        };
    }

    public (FintechEmailIntent.FintechEmailIntentEnum Intent,
        int Score,
        double Confidence) Calculate(string subject, string body)
    {
        var content = Normalize(subject + " " + body);
        var tokens = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var scores = new Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int>();

        foreach (var rule in _rules)
        {
            int score = 0;

            // Strong phrase matching
            foreach (var phrase in rule.Value.StrongPhrases)
            {
                if (content.Contains(phrase))
                    score += rule.Value.PhraseWeight;
            }

            // Token-based matching
            foreach (var token in tokens)
            {
                if (rule.Value.Keywords.Contains(token))
                    score += rule.Value.KeywordWeight;
            }

            // Urgency boost
            if (_urgencyWords.Any(u => content.Contains(u)))
                score += 15;

            if (score > 0)
                scores[rule.Key] = score;
        }

        if (!scores.Any())
            return (FintechEmailIntent.FintechEmailIntentEnum.GeneralInquiry, 0, 0);

        var best = scores.OrderByDescending(x => x.Value).First();
        var totalScore = scores.Values.Sum();

        double confidence = (double)best.Value / totalScore * 100;

        return (best.Key, best.Value, Math.Round(confidence, 2));
    }

    private string Normalize(string text)
    {
        text = text.ToLower();
        text = Regex.Replace(text, @"[^\w\s]", " ");
        text = Regex.Replace(text, @"\s+", " ");
        return text;
    }
}


// using EmailAgent.Worker.Models;
//


// namespace EmailAgent.Worker.Services;
//
//
// public class IntentScorer
// {
//     private readonly new List<IntentRule> _rules;
//     public IntentScorer()
//     {
//         _rules = new List<IntentRule>
//         {
//             new IntentRule
//             {
//                 Label = "Support",
//                 Keywords = new() { "error", "issue", "bug", "failed", "not working" },
//                 Weight = 20
//             },
//             new IntentRule
//             {
//                 Label = "Sales",
//                 Keywords = new() { "quotation", "pricing", "proposal", "purchase" },
//                 Weight = 20
//             },
//             new IntentRule
//             {
//                 Label = "General Query",
//                 Keywords = new() { "Hey", "Hi", "Hello", "Dear" },
//                 Weight = 20
//             },
//             new IntentRule
//             {
//                 Label = "Invoices",
//                 Keywords = new() { "invoice", "payment", "transaction" },
//                 Weight = 20
//             }
//         };
//     }
//     
//     
//     public (string label, int score) Calculate(string subject, string body)
//     {
//         var scores = new Dictionary<string, int>();
//         var content = (subject + " " + body).ToLower();
//     
//         foreach (var rule in _rules)
//         {
//             foreach (var keyword in rule.Keywords)
//             {
//                 if (content.Contains(keyword.ToLower()))
//                 {
//                     if (!scores.ContainsKey(rule.Label))
//                         scores[rule.Label] = 0;
//     
//                     scores[rule.Label] += rule.Weight;
//                 }
//             }
//         }
//     
//         if (!scores.Any())
//             return ("Uncategorized", 0);
//     
//         var best = scores.OrderByDescending(x => x.Value).First();
//     
//         return (best.Key, best.Value);
//     
//     }
//     
//      
//     
// } 