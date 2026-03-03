using System.Text.RegularExpressions;
using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;

/// <summary>
/// Rule-based intent scorer for fintech support emails. Uses phrase matching,
/// keyword scoring, negation awareness, and conflict resolution—no AI model.
/// </summary>
public class IntentScorer
{
    private const int MaxStrongPhraseHitsPerIntent = 2;
    private const int UrgencyBoostCap = 12;
    private const double RunnerUpConfidencePenaltyThreshold = 0.25;

    private readonly List<IntentRuleSet> _ruleSets;
    private readonly List<string> _urgencyWords;
    private readonly List<string> _negationPhrasesForTransactionIssue;
    private readonly List<string> _positivePaymentPhrases;

    public IntentScorer()
    {
        _urgencyWords = new List<string>
        {
            "urgent", "immediately", "asap", "priority", "critical",
            "high priority", "time sensitive", "emergency", "as soon as possible"
        };

        _negationPhrasesForTransactionIssue = new List<string>
        {
            "not credited", "not received", "not reflected", "not processed",
            "not completed", "not received", "deducted but not", "debit but not",
            "without credit", "refund not received", "reversal not processed"
        };

        _positivePaymentPhrases = new List<string>
        {
            "successful", "completed", "credited", "confirmed", "received",
            "payment successful", "transaction successful", "transfer successful"
        };

        _ruleSets = BuildRuleSets();
    }

    public (FintechEmailIntent.FintechEmailIntentEnum Intent, int Score, double Confidence) Calculate(string subject, string body)
    {
        var content = Normalize(subject + " " + body);
        var tokens = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var scores = new Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int>();

        foreach (var ruleSet in _ruleSets)
        {
            int score = 0;
            int strongPhraseHits = 0;

            foreach (var phrase in ruleSet.StrongPhrases)
            {
                if (content.Contains(phrase))
                {
                    score += ruleSet.PhraseWeight;
                    strongPhraseHits++;
                    if (strongPhraseHits >= MaxStrongPhraseHitsPerIntent)
                        break;
                }
            }

            foreach (var phraseKeyword in ruleSet.PhraseKeywords)
            {
                if (content.Contains(phraseKeyword))
                    score += ruleSet.KeywordWeight;
            }

            foreach (var token in tokens)
            {
                if (ruleSet.Keywords.Contains(token))
                    score += ruleSet.KeywordWeight;
            }

            if (ruleSet.NegationBoostPhrases != null)
            {
                foreach (var neg in ruleSet.NegationBoostPhrases)
                {
                    if (content.Contains(neg))
                        score += ruleSet.NegationBoostWeight;
                }
            }

            if (score > 0)
                scores[ruleSet.Intent] = score;
        }

        UrgencyBoost(content, scores);

        ApplyConflictResolution(content, scores);

        if (!scores.Any())
            return (FintechEmailIntent.FintechEmailIntentEnum.GeneralInquiry, 0, 0);

        var ordered = scores.OrderByDescending(x => x.Value).ToList();
        var best = ordered[0];
        int secondScore = ordered.Count > 1 ? ordered[1].Value : 0;

        double confidence = ComputeConfidence(best.Value, secondScore);

        if (confidence < 55 && best.Value < 70)
            return (FintechEmailIntent.FintechEmailIntentEnum.GeneralInquiry, best.Value, Math.Round(confidence, 2));

        return (best.Key, best.Value, Math.Round(confidence, 2));
    }

    private void UrgencyBoost(string content, Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int> scores)
    {
        if (!_urgencyWords.Any(u => content.Contains(u)))
            return;

        int boost = Math.Min(15, UrgencyBoostCap);
        foreach (var intent in scores.Keys.ToList())
            scores[intent] += boost;
    }

    private void ApplyConflictResolution(string content, Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int> scores)
    {
        bool hasNegative = _negationPhrasesForTransactionIssue.Any(content.Contains) ||
                           content.Contains("failed") || content.Contains("declined") || content.Contains("not received");
        bool hasPositive = _positivePaymentPhrases.Any(content.Contains);

        if (hasNegative && hasPositive)
        {
            if (scores.TryGetValue(FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation, out var payScore) && payScore > 0)
                scores[FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation] = Math.Max(0, payScore - 35);
            if (scores.TryGetValue(FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue, out var txScore))
                scores[FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue] = txScore + 20;
        }
    }

    private static double ComputeConfidence(int bestScore, int secondScore)
    {
        if (secondScore <= 0)
            return 95;

        double ratio = (double)secondScore / bestScore;
        if (ratio >= RunnerUpConfidencePenaltyThreshold)
            return (double)bestScore / (bestScore + secondScore) * 100;

        return (double)bestScore / (bestScore + secondScore) * 100;
    }

    private string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.ToLowerInvariant();
        text = Regex.Replace(text, @"['’]", "");

        text = Regex.Replace(text, @"\b(txn|trxn)\b", " transaction ");
        text = Regex.Replace(text, @"\bref\b", " reference ");
        text = Regex.Replace(text, @"\bid\b", " id ");

        text = Regex.Replace(text, @"[^\w\s]", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private List<IntentRuleSet> BuildRuleSets()
    {
        return new List<IntentRuleSet>
        {
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation,
                StrongPhrases = new List<string>
                {
                    "payment completed", "transaction successful", "invoice paid",
                    "fund transfer completed", "amount credited successfully",
                    "payment successful", "transfer completed", "remittance advice",
                    "credited successfully", "payment acknowledgment"
                },
                PhraseKeywords = new List<string>
                {
                    "payment done", "amount paid", "utr number", "transaction reference",
                    "payment reference", "confirmation receipt", "neft", "rtgs", "imps",
                    "payout processed", "settlement done", "invoice cleared"
                },
                Keywords = new List<string>
                {
                    "payment", "transaction", "credited", "utr", "reference",
                    "remittance", "neft", "rtgs", "imps", "transfer", "payout",
                    "settlement", "receipt", "txn", "paid", "debit", "processed"
                },
                PhraseWeight = 42,
                KeywordWeight = 12
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue,
                StrongPhrases = new List<string>
                {
                    "transaction failed", "amount deducted but not credited",
                    "duplicate debit", "payment declined", "pending transaction",
                    "reversal not processed", "double charged", "not credited",
                    "transaction stuck", "payment stuck", "gateway error"
                },
                PhraseKeywords = new List<string>
                {
                    "failed transaction", "incorrect debit", "wrong deduction",
                    "timeout error", "processing error", "unsuccessful payment",
                    "technical glitch", "system failure", "charge not reflected"
                },
                Keywords = new List<string>
                {
                    "failed", "pending", "error", "declined", "deducted",
                    "stuck", "timeout", "gateway", "processing", "double charged",
                    "incorrect", "unsuccessful", "reversal", "duplicate", "stuck"
                },
                NegationBoostPhrases = new List<string> { "not credited", "not received", "not reflected", "not processed" },
                PhraseWeight = 48,
                KeywordWeight = 16,
                NegationBoostWeight = 10
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.RefundRequest,
                StrongPhrases = new List<string>
                {
                    "refund request", "money back", "reverse transaction",
                    "refund not received", "initiate refund", "cancel transaction",
                    "refund pending", "credit back", "charge reversal"
                },
                PhraseKeywords = new List<string>
                {
                    "request refund", "refund status", "refund initiated",
                    "refund processed", "refund delay", "amount reversal"
                },
                Keywords = new List<string>
                {
                    "refund", "reversal", "cancel", "return", "credit back",
                    "refund status", "refund pending", "money back", "reverse"
                },
                NegationBoostPhrases = new List<string> { "refund not received", "refund not processed" },
                PhraseWeight = 40,
                KeywordWeight = 18,
                NegationBoostWeight = 8
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.FraudAlert,
                StrongPhrases = new List<string>
                {
                    "unauthorized transaction", "account compromised",
                    "fraudulent activity", "security breach", "unknown debit",
                    "card misuse", "identity theft", "suspicious transaction"
                },
                PhraseKeywords = new List<string>
                {
                    "fraud alert", "phishing", "blocked card", "risk alert",
                    "unauthorized debit", "unknown charge", "security concern"
                },
                Keywords = new List<string>
                {
                    "fraud", "unauthorized", "suspicious", "unknown debit",
                    "phishing", "identity theft", "misuse", "blocked", "risk alert"
                },
                PhraseWeight = 55,
                KeywordWeight = 22
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.KYCVerification,
                StrongPhrases = new List<string>
                {
                    "kyc verification", "kyc update", "identity verification",
                    "document verification", "kyc pending", "kyc rejected",
                    "document submission", "compliance verification"
                },
                PhraseKeywords = new List<string>
                {
                    "address proof", "pan card", "aadhaar", "passport copy",
                    "upload documents", "kyc approved", "identity proof"
                },
                Keywords = new List<string>
                {
                    "kyc", "verification", "identity", "address proof", "pan",
                    "aadhaar", "passport", "documents", "compliance", "upload"
                },
                PhraseWeight = 38,
                KeywordWeight = 14
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.ComplianceRequest,
                StrongPhrases = new List<string>
                {
                    "regulatory report", "audit requirement", "compliance document",
                    "aml", "rbi circular", "sox compliance", "regulatory filing",
                    "compliance review", "internal audit", "risk assessment"
                },
                PhraseKeywords = new List<string>
                {
                    "legal notice", "regulatory", "audit", "aml", "rbi", "sox"
                },
                Keywords = new List<string>
                {
                    "compliance", "audit", "regulatory", "aml", "rbi", "sox",
                    "filing", "legal", "assessment"
                },
                PhraseWeight = 40,
                KeywordWeight = 18
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.ChargebackDispute,
                StrongPhrases = new List<string>
                {
                    "chargeback", "dispute raised", "card dispute",
                    "dispute case", "chargeback notice", "representment",
                    "retrieval request", "card network dispute"
                },
                PhraseKeywords = new List<string> { "dispute reference", "reason code" },
                Keywords = new List<string>
                {
                    "chargeback", "dispute", "retrieval", "representment",
                    "reason code", "card network"
                },
                PhraseWeight = 42,
                KeywordWeight = 20
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.SettlementQuery,
                StrongPhrases = new List<string>
                {
                    "settlement report", "merchant payout", "reconciliation report",
                    "settlement pending", "payout delay", "daily settlement",
                    "settlement amount", "merchant statement", "payout cycle"
                },
                PhraseKeywords = new List<string>
                {
                    "weekly settlement", "monthly reconciliation", "settlement summary"
                },
                Keywords = new List<string>
                {
                    "settlement", "reconciliation", "payout", "merchant",
                    "statement", "cycle", "report"
                },
                PhraseWeight = 38,
                KeywordWeight = 14
            },
            new()
            {
                Intent = FintechEmailIntent.FintechEmailIntentEnum.AccountAccessIssue,
                StrongPhrases = new List<string>
                {
                    "unable to login", "account blocked", "otp not received",
                    "password reset", "account locked", "access denied",
                    "authentication failed", "login issue", "mfa problem"
                },
                PhraseKeywords = new List<string>
                {
                    "reset credentials", "login failed", "cannot login"
                },
                Keywords = new List<string>
                {
                    "login", "blocked", "otp", "password", "reset", "access denied",
                    "authentication", "mfa", "locked", "credentials"
                },
                PhraseWeight = 36,
                KeywordWeight = 14
            }
        };
    }

    private class IntentRuleSet
    {
        public FintechEmailIntent.FintechEmailIntentEnum Intent { get; init; }
        public List<string> StrongPhrases { get; init; } = new();
        public List<string> PhraseKeywords { get; init; } = new();
        public List<string> Keywords { get; init; } = new();
        public List<string>? NegationBoostPhrases { get; init; }
        public int PhraseWeight { get; init; }
        public int KeywordWeight { get; init; }
        public int NegationBoostWeight { get; init; }
    }
}
