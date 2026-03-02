using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;

public class FintechIntentEngine
{
     // private readonly List<(FintechEmailIntent.FintechEmailIntentEnum Intent, List<string> Keywords, int Weight)> _rules;

    // public FintechIntentEngine()
    // {
    //     _rules = new()
    //     {
    //         (FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue,
    //             new() { "failed transaction", "transaction failed", "pending transaction", "not credited", "incorrect debit", "double charged" }, 30),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation,
    //             new() { "payment completed", "transaction successful", "payment done", "transfer completed" }, 25),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.RefundRequest,
    //             new() { "refund request", "refund not received", "initiate refund", "money back", "cancel transaction" }, 30),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.KYCVerification,
    //             new() { "kyc documents", "kyc verification", "identity proof", "address proof", "pan card", "aadhaar" }, 25),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.ComplianceRequest,
    //             new() { "audit requirement", "regulatory report", "compliance document", "sox", "aml", "rbi" }, 35),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.ChargebackDispute,
    //             new() { "chargeback", "dispute raised", "card dispute", "merchant dispute" }, 35),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.SettlementQuery,
    //             new() { "settlement report", "reconciliation", "settlement pending", "merchant payout" }, 25),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.FraudAlert,
    //             new() { "unauthorized transaction", "fraudulent activity", "suspicious transaction", "account compromised" }, 40),
    //
    //         (FintechEmailIntent.FintechEmailIntentEnum.AccountAccessIssue,
    //             new() { "account blocked", "unable to login", "otp not received", "password reset" }, 25)
    //     };
    // }
    //
    // public (FintechEmailIntent.FintechEmailIntentEnum Intent, int Score) Detect(string subject, string body)
    // {
    //     var content = (subject + " " + body).ToLower();
    //     var scores = new Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int>();
    //
    //     foreach (var rule in _rules)
    //     {
    //         foreach (var keyword in rule.Keywords)
    //         {
    //             if (content.Contains(keyword))
    //             {
    //                 if (!scores.ContainsKey(rule.Intent))
    //                     scores[rule.Intent] = 0;
    //
    //                 scores[rule.Intent] += rule.Weight;
    //             }
    //         }
    //     }
    //
    //     if (!scores.Any())
    //         return (FintechEmailIntent.FintechEmailIntentEnum.GeneralInquiry, 0);
    //
    //     var best = scores.OrderByDescending(x => x.Value).First();
    //     return (best.Key, best.Value);
    // }
    
    
       private readonly Dictionary<FintechEmailIntent.FintechEmailIntentEnum, (List<string> Keywords, int Weight)> _rules;

    public  FintechIntentEngine()
    {
        _rules = new ()
        {
            {
                FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation,
                (
                    new List<string>
                    {
                        // Core payment confirmation
                        "payment completed", "payment done", "payment successful",
                        "transaction successful", "transaction completed",
                        "transfer completed", "funds transferred",
                        "amount credited", "amount paid",
                        "remittance advice", "proof of payment",
                        "utr number", "transaction reference",
                        "payment reference id", "confirmation receipt",

                        // Banking language
                        "bank transfer", "neft", "rtgs", "imps",
                        "swift transfer", "wire transfer",
                        "credited successfully", "debit processed",
                        "payout processed", "settlement done",

                        // Invoice-linked
                        "invoice paid", "invoice cleared",
                        "payment against invoice", "invoice settlement",

                        // Variations
                        "amount has been paid", "successfully processed",
                        "payment initiated", "fund transfer completed",
                        "transaction id", "txn id", "reference number",

                        // Short variations
                        "paid today", "payment processed",
                        "payment acknowledgment", "acknowledge payment"
                    },
                    25
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue,
                (
                    new List<string>
                    {
                        "transaction failed", "failed transaction",
                        "pending transaction", "not credited",
                        "incorrect debit", "double charged",
                        "amount deducted but not credited",
                        "debit without credit",
                        "transaction declined", "payment declined",
                        "timeout error", "processing error",
                        "gateway error", "network error",
                        "unexpected debit", "wrong deduction",
                        "reversal not processed",
                        "transaction stuck", "payment stuck",
                        "charge not reflected",
                        "duplicate debit", "duplicate transaction",
                        "technical glitch", "system failure",
                        "transaction error", "unsuccessful payment"
                    },
                    30
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.RefundRequest,
                (
                    new List<string>
                    {
                        "refund request", "request refund",
                        "initiate refund", "refund not received",
                        "money back", "return payment",
                        "reverse transaction", "cancel transaction",
                        "refund pending", "refund status",
                        "refund initiated", "refund processed",
                        "refund confirmation", "refund delay",
                        "charge reversal", "amount reversal",
                        "credit back", "refund amount",
                        "refund reference", "refund txn id"
                    },
                    30
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.FraudAlert,
                (
                    new List<string>
                    {
                        "unauthorized transaction", "fraudulent activity",
                        "suspicious transaction", "account compromised",
                        "unknown debit", "card misuse",
                        "fraud alert", "security breach",
                        "identity theft", "phishing",
                        "scam transaction", "unauthorized debit",
                        "blocked card", "freeze account",
                        "charge not recognized", "unknown charge",
                        "security concern", "risk alert"
                    },
                    40
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.KYCVerification,
                (
                    new List<string>
                    {
                        "kyc update", "kyc verification",
                        "identity verification", "address proof",
                        "pan card", "aadhaar", "passport copy",
                        "document verification", "upload documents",
                        "compliance verification", "kyc rejected",
                        "kyc pending", "kyc approved",
                        "document submission", "identity proof"
                    },
                    25
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.SettlementQuery,
                (
                    new List<string>
                    {
                        "settlement report", "merchant payout",
                        "reconciliation report", "settlement pending",
                        "payout delay", "daily settlement",
                        "weekly settlement", "monthly reconciliation",
                        "settlement amount", "merchant statement",
                        "payout cycle", "settlement summary"
                    },
                    25
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.ChargebackDispute,
                (
                    new List<string>
                    {
                        "chargeback", "dispute raised",
                        "card dispute", "dispute case",
                        "retrieval request", "representment",
                        "chargeback notice", "dispute reference",
                        "reason code", "card network dispute"
                    },
                    35
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.ComplianceRequest,
                (
                    new List<string>
                    {
                        "regulatory report", "audit requirement",
                        "compliance document", "aml",
                        "rbi circular", "sox compliance",
                        "internal audit", "risk assessment",
                        "regulatory filing", "legal notice",
                        "compliance review"
                    },
                    35
                )
            },

            {
                FintechEmailIntent.FintechEmailIntentEnum.AccountAccessIssue,
                (
                    new List<string>
                    {
                        "unable to login", "account blocked",
                        "otp not received", "password reset",
                        "account locked", "access denied",
                        "reset credentials", "authentication failed",
                        "login issue", "mfa problem"
                    },
                    25
                )
            }
        };
    }

    public (FintechEmailIntent.FintechEmailIntentEnum Intent, int Score) Detect(string subject, string body)
    {
        var content = (subject + " " + body).ToLower();
        var scores = new Dictionary<FintechEmailIntent.FintechEmailIntentEnum, int>();

        foreach (var rule in _rules)
        {
            foreach (var keyword in rule.Value.Keywords)
            {
                if (content.Contains(keyword))
                {
                    if (!scores.ContainsKey(rule.Key))
                        scores[rule.Key] = 0;

                    scores[rule.Key] += rule.Value.Weight;
                }
            }
        }

        if (!scores.Any())
            return (FintechEmailIntent.FintechEmailIntentEnum.GeneralInquiry, 0);

        var best = scores.OrderByDescending(x => x.Value).First();
        return (best.Key, best.Value);
    }
}