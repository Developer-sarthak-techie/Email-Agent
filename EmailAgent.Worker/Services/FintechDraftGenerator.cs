using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;

/// <summary>
/// Generates domain-appropriate draft responses for fintech support intents
/// using multiple template variations per intent.
/// </summary>
public class FintechDraftGenerator
{
    private static readonly Random _random = Random.Shared;

    public string Generate(FintechEmailIntent.FintechEmailIntentEnum intent, string customerName)
    {
        var name = string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName.Trim();
        var templates = GetTemplates(intent);
        var template = templates[_random.Next(templates.Count)];
        return string.Format(template, name);
    }

    private static IReadOnlyList<string> GetTemplates(FintechEmailIntent.FintechEmailIntentEnum intent)
    {
        return intent switch
        {
            FintechEmailIntent.FintechEmailIntentEnum.PaymentConfirmation => PaymentConfirmationTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue => TransactionIssueTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.RefundRequest => RefundRequestTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.FraudAlert => FraudAlertTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.KYCVerification => KycVerificationTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.ComplianceRequest => ComplianceRequestTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.ChargebackDispute => ChargebackDisputeTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.SettlementQuery => SettlementQueryTemplates,
            FintechEmailIntent.FintechEmailIntentEnum.AccountAccessIssue => AccountAccessIssueTemplates,
            _ => GeneralInquiryTemplates
        };
    }

    // {0} = customer name

    private static readonly IReadOnlyList<string> PaymentConfirmationTemplates = new[]
    {
        "Dear {0},\n\nThank you for your message regarding the payment/transaction. We confirm that your payment has been received and processed successfully. If you need a formal receipt or transaction reference for your records, please reply with the transaction date and amount and we will share the details.\n\nRegards,\nFinance & Operations Team",
        "Dear {0},\n\nWe have received your communication and confirm that the payment has been processed successfully. For a receipt or transaction reference, please share the transaction date and amount and we will provide the details at the earliest.\n\nRegards,\nFinance & Operations Team",
        "Dear {0},\n\nThank you for reaching out. Your payment has been received and credited as per our records. If you need a confirmation or reference for your records, reply with the date and amount and we will send the same.\n\nRegards,\nFinance & Operations Team"
    };

    private static readonly IReadOnlyList<string> TransactionIssueTemplates = new[]
    {
        "Dear {0},\n\nWe have received your query regarding the transaction. Our team is reviewing the matter and will update you after verification. If you have a UTR/reference number or transaction ID, please share it to help us expedite the check.\n\nRegards,\nFintech Support Team",
        "Dear {0},\n\nThank you for writing to us. Your transaction concern has been logged and is under review. To speed this up, please share the UTR, reference number, or transaction ID if available. We will revert with an update shortly.\n\nRegards,\nFintech Support Team",
        "Dear {0},\n\nWe have noted your issue with the transaction. Our team is looking into it and will get back to you after verification. Kindly share the transaction reference or UTR so we can trace and resolve it quickly.\n\nRegards,\nFintech Support Team"
    };

    private static readonly IReadOnlyList<string> RefundRequestTemplates = new[]
    {
        "Dear {0},\n\nYour refund request has been noted. We are currently validating the transaction details and will revert with the next steps shortly. Refunds are typically processed within 5–7 business days once approved.\n\nRegards,\nFinance Team",
        "Dear {0},\n\nThank you for your email. We have registered your refund request and are validating the transaction. You will receive an update on the next steps soon. Approved refunds are usually processed within 5–7 business days.\n\nRegards,\nFinance Team",
        "Dear {0},\n\nWe have received your request for a refund. Our team is verifying the transaction and will revert with the status and further steps. Refund processing typically takes 5–7 business days after approval.\n\nRegards,\nFinance Team"
    };

    private static readonly IReadOnlyList<string> FraudAlertTemplates = new[]
    {
        "Dear {0},\n\nWe take security seriously. Your concern regarding the suspicious transaction has been escalated for immediate review. If you have not already done so, we recommend securing your account (change password, review linked devices) and reporting to your bank if needed.\n\nRegards,\nRisk & Security Team",
        "Dear {0},\n\nThank you for alerting us. This has been escalated to our Risk & Security team for immediate review. We advise you to secure your account (password change, review linked devices) and contact your bank if you have not already.\n\nRegards,\nRisk & Security Team",
        "Dear {0},\n\nWe have escalated your security concern for urgent review. Please ensure your account is secured (update password, check linked devices) and inform your bank if required. Our team will follow up with you shortly.\n\nRegards,\nRisk & Security Team"
    };

    private static readonly IReadOnlyList<string> KycVerificationTemplates = new[]
    {
        "Dear {0},\n\nWe have received your KYC-related communication. Our verification team will process the documents and notify you upon completion. If your submission was rejected, we will share the reason and next steps separately.\n\nRegards,\nCompliance Team",
        "Dear {0},\n\nThank you for your KYC submission. Our team is processing the documents and will notify you once verification is complete. In case of rejection, we will communicate the reason and next steps.\n\nRegards,\nCompliance Team",
        "Dear {0},\n\nYour KYC documents have been received and are under verification. We will update you on completion. If there are any issues with the submission, we will share the details and required actions.\n\nRegards,\nCompliance Team"
    };

    private static readonly IReadOnlyList<string> ComplianceRequestTemplates = new[]
    {
        "Dear {0},\n\nYour compliance/regulatory request has been received and forwarded to our compliance team. We will respond within the stated timelines as per our policy and regulatory requirements.\n\nRegards,\nCompliance Team",
        "Dear {0},\n\nWe have received your request and forwarded it to the compliance team. A response will be provided within the applicable timelines in line with our policy and regulatory requirements.\n\nRegards,\nCompliance Team",
        "Dear {0},\n\nThank you for your communication. Your compliance/regulatory query has been logged and assigned to our compliance team. We will revert within the stipulated timelines.\n\nRegards,\nCompliance Team"
    };

    private static readonly IReadOnlyList<string> ChargebackDisputeTemplates = new[]
    {
        "Dear {0},\n\nWe have received your dispute/chargeback communication. Our disputes team will review the case and respond with the status and any required documentation within the applicable timeframe.\n\nRegards,\nDisputes & Chargebacks Team",
        "Dear {0},\n\nThank you for writing to us. Your dispute has been logged and is with our chargebacks team. We will review the case and share the status and any documentation required within the applicable timeframe.\n\nRegards,\nDisputes & Chargebacks Team",
        "Dear {0},\n\nYour chargeback/dispute has been received and assigned to our disputes team. We will review and revert with the status and next steps, including any documentation needed, within the applicable period.\n\nRegards,\nDisputes & Chargebacks Team"
    };

    private static readonly IReadOnlyList<string> SettlementQueryTemplates = new[]
    {
        "Dear {0},\n\nYour settlement/reconciliation query has been noted. Our settlements team will verify the details and share the report or clarification as applicable.\n\nRegards,\nSettlements Team",
        "Dear {0},\n\nWe have received your query regarding settlement/reconciliation. Our team will verify and share the relevant report or clarification at the earliest.\n\nRegards,\nSettlements Team",
        "Dear {0},\n\nThank you for your email. Your settlement or reconciliation request has been forwarded to our settlements team. We will revert with the report or clarification as applicable.\n\nRegards,\nSettlements Team"
    };
    

    private static readonly IReadOnlyList<string> AccountAccessIssueTemplates = new[]
    {
        "Dear {0},\n\nWe have received your request regarding account access. Our team will assist with login/OTP/password reset as applicable. If the issue persists, we will escalate to technical support and get back to you shortly.\n\nRegards,\nSupport Team",
        "Dear {0},\n\nThank you for reaching out. Your account access issue has been logged. We will help with login, OTP, or password reset as needed and escalate to technical support if required.\n\nRegards,\nSupport Team",
        "Dear {0},\n\nWe have noted your account access concern. Our team will assist with login, OTP, or password reset. If the problem continues, we will escalate to our technical team and revert shortly.\n\nRegards,\nSupport Team"
    };

    private static readonly IReadOnlyList<string> GeneralInquiryTemplates = new[]
    {
        "Dear {0},\n\nThank you for reaching out. We are reviewing your email and will respond shortly.\n\nRegards,\nTeam",
        "Dear {0},\n\nWe have received your message and are looking into it. We will get back to you at the earliest.\n\nRegards,\nTeam",
        "Dear {0},\n\nThank you for writing to us. Your email is under review and we will respond soon.\n\nRegards,\nTeam"
    };
}
