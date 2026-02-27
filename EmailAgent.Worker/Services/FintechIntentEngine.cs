using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;

public class FintechIntentEngine
{
     private readonly List<(FintechEmailIntent.FintechEmailIntentEnum Intent, List<string> Keywords, int Weight)> _rules;

    public FintechIntentEngine()
    {
        _rules = new()
        {
            (FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue,
                new() { "failed transaction", "transaction failed", "pending transaction", "not credited", "incorrect debit", "double charged" }, 30),

            (FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation,
                new() { "payment completed", "transaction successful", "payment done", "transfer completed" }, 25),

            (FintechEmailIntent.FintechEmailIntentEnum.RefundRequest,
                new() { "refund request", "refund not received", "initiate refund", "money back", "cancel transaction" }, 30),

            (FintechEmailIntent.FintechEmailIntentEnum.KYCVerification,
                new() { "kyc documents", "kyc verification", "identity proof", "address proof", "pan card", "aadhaar" }, 25),

            (FintechEmailIntent.FintechEmailIntentEnum.ComplianceRequest,
                new() { "audit requirement", "regulatory report", "compliance document", "sox", "aml", "rbi" }, 35),

            (FintechEmailIntent.FintechEmailIntentEnum.ChargebackDispute,
                new() { "chargeback", "dispute raised", "card dispute", "merchant dispute" }, 35),

            (FintechEmailIntent.FintechEmailIntentEnum.SettlementQuery,
                new() { "settlement report", "reconciliation", "settlement pending", "merchant payout" }, 25),

            (FintechEmailIntent.FintechEmailIntentEnum.FraudAlert,
                new() { "unauthorized transaction", "fraudulent activity", "suspicious transaction", "account compromised" }, 40),

            (FintechEmailIntent.FintechEmailIntentEnum.AccountAccessIssue,
                new() { "account blocked", "unable to login", "otp not received", "password reset" }, 25)
        };
    }

    public (FintechEmailIntent.FintechEmailIntentEnum Intent, int Score) Detect(string subject, string body)
    {
        var content = (subject + " " + body).ToLower();
        var scores = new Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int>();

        foreach (var rule in _rules)
        {
            foreach (var keyword in rule.Keywords)
            {
                if (content.Contains(keyword))
                {
                    if (!scores.ContainsKey(rule.Intent))
                        scores[rule.Intent] = 0;

                    scores[rule.Intent] += rule.Weight;
                }
            }
        }

        if (!scores.Any())
            return (FintechEmailIntent.FintechEmailIntentEnum.GeneralInquiry, 0);

        var best = scores.OrderByDescending(x => x.Value).First();
        return (best.Key, best.Value);
    }
}