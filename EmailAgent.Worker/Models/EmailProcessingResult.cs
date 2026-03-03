namespace EmailAgent.Worker.Models;

/// <summary>
/// Result of processing a single email through the validator (sender rules, intent scoring, draft, move).
/// </summary>
public class EmailProcessingResult
{
    /// <summary>True if processing completed without error (email was handled as per rules).</summary>
    public bool Success { get; init; }

    /// <summary>Label/folder the email was moved to, if any.</summary>
    public string? Label { get; init; }

    /// <summary>Human-readable outcome message for logging or API response.</summary>
    public string? Message { get; init; }

    /// <summary>Detected intent when scoring was applied; null for sender-rule-only moves (e.g. Jira, AWS).</summary>
    public FintechEmailIntent.FintechEmailIntentEnum? Intent { get; init; }

    /// <summary>Intent score when scoring was applied.</summary>
    public int? Score { get; init; }

    /// <summary>Intent confidence percentage when scoring was applied.</summary>
    public double? Confidence { get; init; }
}
