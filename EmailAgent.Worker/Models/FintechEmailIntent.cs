namespace EmailAgent.Worker.Models;

public class FintechEmailIntent
{
    public enum FintechEmailIntentEnum
    {
        TransactionIssue,
        PaymentConfirmation,
        RefundRequest,
        KYCVerification,
        ComplianceRequest,
        ChargebackDispute,
        SettlementQuery,
        FraudAlert,
        AccountAccessIssue,
        /// <summary>BRD: SOA / Capital Gain Statement requests (Zoho category: Statements).</summary>
        StatementRequest,
        /// <summary>BRD: Unregistered email or missing PAN/folio (request identification).</summary>
        UnregisteredOrIncompleteInfo,
        /// <summary>BRD: Redemption queries / delay (Zoho: Redemption → Delay).</summary>
        RedemptionQuery,
        /// <summary>BRD: NAV, SIP, fund details, dividend (FAQ/knowledge-base).</summary>
        NavSipFundQuery,
        GeneralInquiry
    }
}