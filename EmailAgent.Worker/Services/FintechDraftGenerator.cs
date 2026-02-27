using EmailAgent.Worker.Models;

namespace EmailAgent.Worker.Services;

public class FintechDraftGenerator
{
    public string Generate(FintechEmailIntent.FintechEmailIntentEnum intent, string customerName)
    {
        return intent switch
        {
            FintechEmailIntent.FintechEmailIntentEnum.TransactionIssue =>
                $"Dear {customerName},\n\nWe have received your query regarding the transaction. Our team is reviewing the matter and will update you after verification.\n\nRegards,\nFintech Support Team",

            FintechEmailIntent.FintechEmailIntentEnum.RefundRequest =>
                $"Dear {customerName},\n\nYour refund request has been noted. We are currently validating the transaction details and will revert with the next steps shortly.\n\nRegards,\nFinance Team",

            FintechEmailIntent.FintechEmailIntentEnum.FraudAlert =>
                $"Dear {customerName},\n\nWe take security seriously. Your concern regarding the suspicious transaction has been escalated for immediate review.\n\nRegards,\nRisk & Security Team",

            FintechEmailIntent.FintechEmailIntentEnum.KYCVerification =>
                $"Dear {customerName},\n\nWe have received your KYC-related communication. Our verification team will process the documents and notify you upon completion.\n\nRegards,\nCompliance Team",

            _ =>
                $"Dear {customerName},\n\nThank you for reaching out. We are reviewing your email and will respond shortly.\n\nRegards,\nTeam"
        };
    }
    
}