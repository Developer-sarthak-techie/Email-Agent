using EmailAgent.Worker.Models;
using MailKit;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker.Services;

/// <summary>
/// Performs all email validation and processing: sender rules, intent scoring, draft creation, and move to label.
/// Can be used by the Worker or by a future API controller without depending on the Worker.
/// Resilient: input validation, try/catch, optional retry with backoff.
/// </summary>
public interface IEmailProcessingValidatorService
{
    /// <summary>
    /// Runs sender checks, intent scoring, and (when applicable) creates draft and moves email to label.
    /// </summary>
    /// <param name="email">The fetched email to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with Success, Label, Message, and optional Intent/Score/Confidence.</returns>
    Task<EmailProcessingResult> ProcessAndValidateAsync(FetchedEmail email, CancellationToken cancellationToken = default);
}

public class EmailProcessingValidatorService : IEmailProcessingValidatorService
{
    private readonly IEmailMover _mover;
    private readonly IEmailLabelResolverService _labelResolver;
    private readonly IEmailDraftService _draftService;
    private readonly IntentScorer _scorer;
    private readonly FintechDraftGenerator _draftGenerator;
    private readonly ILogger<EmailProcessingValidatorService> _logger;
    private readonly ProcessingEngineOptions _options;

    private const int ScoreThreshold = 50;

    /// <summary>CC address for high-priority tagged drafts (e.g. @himanshu, @sebi, @radhika).</summary>
    private const string HighPriorityDraftCc = "s@binmile.com";

    private static readonly string[] HighPriorityMentions = { "@himanshu", "@sebi", "@radhika" };

    /// <summary>Keywords that indicate a statement / SOA / info request as per BRD.</summary>
    private static readonly string[] ExternalStatementOrInfoKeywords =
    {
        "soa", "statement of account", "capital gain statement", "capital gains statement",
        "capital gain", "capital gains", "statement", "nav", "sip", "fund details",
        "dividend", "statement request"
    };

    public EmailProcessingValidatorService(
        IEmailMover mover,
        IEmailLabelResolverService labelResolver,
        IEmailDraftService draftService,
        IntentScorer scorer,
        FintechDraftGenerator draftGenerator,
        IOptions<ProcessingEngineOptions> options,
        ILogger<EmailProcessingValidatorService> logger)
    {
        _mover = mover;
        _labelResolver = labelResolver;
        _draftService = draftService;
        _scorer = scorer;
        _draftGenerator = draftGenerator;
        _options = options?.Value ?? new ProcessingEngineOptions();
        _logger = logger;
    }

    public async Task<EmailProcessingResult> ProcessAndValidateAsync(FetchedEmail email, CancellationToken cancellationToken = default)
    {
        if (email == null)
        {
            _logger.LogWarning("ProcessAndValidateAsync called with null email");
            return new EmailProcessingResult { Success = false, Message = "Invalid email: null." };
        }

        if (email.Message == null)
        {
            _logger.LogWarning("ProcessAndValidateAsync called with null email.Message for UID {Uid}", email.Uid);
            return new EmailProcessingResult { Success = false, Message = "Invalid email: message is null." };
        }

        var retries = Math.Max(0, Math.Min(_options.RetryCount, 5));
        var delayMs = Math.Max(100, Math.Min(_options.RetryDelayMs, 30_000));

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await ProcessOneAsync(email, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Process attempt {Attempt}/{Total} failed for MessageId {MessageId}",
                    attempt + 1, retries + 1, email.Message.MessageId);

                if (attempt == retries)
                {
                    _logger.LogError(ex, "All retries exhausted for MessageId {MessageId}", email.Message.MessageId);
                    return new EmailProcessingResult
                    {
                        Success = false,
                        Message = $"Processing failed after {retries + 1} attempt(s): {ex.Message}"
                    };
                }

                var backoff = TimeSpan.FromMilliseconds(delayMs * Math.Pow(2, attempt));
                await Task.Delay(backoff, cancellationToken);
            }
        }

        return new EmailProcessingResult { Success = false, Message = "Processing failed (retries exhausted)." };
    }

    private async Task<EmailProcessingResult> ProcessOneAsync(FetchedEmail email, CancellationToken cancellationToken)
    {
        var from = email.Message.From?.ToString() ?? "";
        var subject = email.Message.Subject ?? "";
        var body = email.Message.TextBody ?? "";
        var subjectAndBody = subject + " " + body;

        // Extra layer: external (non-binmile.com) users requesting statements / SOA / info
        var senderAddress = email.Message.From?.Mailboxes.FirstOrDefault()?.Address ?? "";
        var isInternalSender = senderAddress.EndsWith("@binmile.com", StringComparison.OrdinalIgnoreCase);
        var loweredContent = subjectAndBody.ToLowerInvariant();

        if (!isInternalSender && ExternalStatementOrInfoKeywords.Any(k => loweredContent.Contains(k)))
        {
            var senderName = email.Message.From?.Mailboxes.FirstOrDefault()?.Name ?? "Customer";
            var draftBody =
                $"Dear {senderName},\n\nKindly write to us from your registered email address and provide your PAN and folio number so that we can share the statement or information you are requesting.\n\nRegards,\nInvestor Services";

            await _draftService.CreateDraftReplyAsync(email.Message, draftBody, null, cancellationToken);

            var labelName = _labelResolver.GetLabelNameForIntent(FintechEmailIntent.FintechEmailIntentEnum.UnregisteredOrIncompleteInfo);
            await EnsureLabelAndMoveAsync(email.Uid, labelName, cancellationToken);

            _logger.LogInformation(
                "External statement/info request detected from {Address}. Draft created asking for registered email + PAN/folio, moved to label: {Label}",
                senderAddress, labelName);

            return new EmailProcessingResult
            {
                Success = true,
                Label = labelName,
                Message = "External statement/info request: asked user to write from registered email with PAN + folio.",
                Intent = FintechEmailIntent.FintechEmailIntentEnum.UnregisteredOrIncompleteInfo
            };
        }

        // Priority rule 1: @himanshu, @sebi, @radhika in subject or body → HIGH PRIORITY + draft with CC
        if (HighPriorityMentions.Any(m => subjectAndBody.Contains(m, StringComparison.OrdinalIgnoreCase)))
        {
            var senderName = email.Message.From?.Mailboxes.FirstOrDefault()?.Name ?? "Customer";
            var draftBody = _draftGenerator.GenerateHighPriorityTaggedDraft(senderName);
            await _draftService.CreateDraftReplyAsync(email.Message, draftBody, new[] { HighPriorityDraftCc }, cancellationToken);

            await EnsureLabelAndMoveAsync(email.Uid, "HIGH PRIORITY", cancellationToken);
            _logger.LogInformation("High-priority tagged: draft created (CC s@binmile.com), moved to HIGH PRIORITY");
            return new EmailProcessingResult
            {
                Success = true,
                Label = "HIGH PRIORITY",
                Message = "High-priority tagged: draft created (CC s@binmile.com), moved to HIGH PRIORITY"
            };
        }

        // Priority rule 2: Jira sender
        if (from.Contains("@taiservices.atlassian.net", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureLabelAndMoveAsync(email.Uid, "Jira", cancellationToken);
            _logger.LogInformation("Moved to Jira (sender rule matched)");
            return new EmailProcessingResult
            {
                Success = true,
                Label = "Jira",
                Message = "Moved to Jira (sender rule matched)"
            };
        }

        // Priority rule 3: AWS sender
        if (from.Contains("no-reply@sns.amazonaws.com", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureLabelAndMoveAsync(email.Uid, "AWS", cancellationToken);
            _logger.LogInformation("Moved to AWS (sender rule matched)");
            return new EmailProcessingResult
            {
                Success = true,
                Label = "AWS",
                Message = "Moved to AWS (sender rule matched)"
            };
        }

        // Intent-based processing
        var (intent, score, confidence) = _scorer.Calculate(subject, body);
        _logger.LogInformation(
            "Intent: {Intent} | Score: {Score} | Confidence: {Confidence}%",
            intent, score, confidence);

        if (score >= ScoreThreshold)
        {
            var senderName = email.Message.From?.Mailboxes.FirstOrDefault()?.Name ?? "Customer";
            var draft = _draftGenerator.Generate(intent, senderName);
            await _draftService.CreateDraftReplyAsync(email.Message, draft, null, cancellationToken);

            var labelName = _labelResolver.GetLabelNameForIntent(intent);
            await _labelResolver.EnsureLabelExistsAsync(labelName, cancellationToken);
            await _mover.MoveToLabelAsync(email.Uid, labelName);

            _logger.LogInformation("Draft created and moved to label: {Label}", labelName);
            return new EmailProcessingResult
            {
                Success = true,
                Label = labelName,
                Message = $"Draft created and moved to label: {labelName}",
                Intent = intent,
                Score = score,
                Confidence = confidence
            };
        }

        _logger.LogInformation(
            "No action for this email: score {Score} (threshold {Threshold}). Kept in Inbox. Intent: {Intent}",
            score, ScoreThreshold, intent);
        return new EmailProcessingResult
        {
            Success = true,
            Message = "Score below threshold. Kept in Inbox.",
            Intent = intent,
            Score = score,
            Confidence = confidence
        };
    }

    private async Task EnsureLabelAndMoveAsync(UniqueId uid, string labelName, CancellationToken cancellationToken)
    {
        await _labelResolver.EnsureLabelExistsAsync(labelName, cancellationToken);
        await _mover.MoveToLabelAsync(uid, labelName, cancellationToken);
    }
}
