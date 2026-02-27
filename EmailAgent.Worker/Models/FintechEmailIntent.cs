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
        GeneralInquiry
    }
}