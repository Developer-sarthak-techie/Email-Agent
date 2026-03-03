namespace EmailAgent.Worker.Models;

/// <summary>
/// Options for high-throughput email processing (batch size, parallelism, timeouts, retries).
/// </summary>
public class ProcessingEngineOptions
{
    public const string SectionName = "ProcessingEngine";

    /// <summary>Max unread emails to fetch per run. Prevents memory blow-up (e.g. 10_000+). Default 2000.</summary>
    public int BatchSize { get; set; } = 2000;

    /// <summary>Max concurrent emails processed at once. Balance speed vs IMAP connection limits. Default 20.</summary>
    public int MaxDegreeOfParallelism { get; set; } = 20;

    /// <summary>Timeout per email in seconds. Prevents a single stuck email from blocking. Default 90.</summary>
    public int PerEmailTimeoutSeconds { get; set; } = 90;

    /// <summary>Number of retries per email on transient failure. Default 2.</summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>Delay in ms before first retry. Exponential backoff: delay * 2^attempt. Default 1000.</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>IMAP connect/read timeout in seconds. Default 30.</summary>
    public int ImapTimeoutSeconds { get; set; } = 30;
}
