using EmailAgent.Worker.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailAgent.Worker.Services;

public interface IEmailReaderService
{
    /// <summary>
    /// Fetches unread emails from inbox, optionally limited to maxCount for batch processing.
    /// </summary>
    /// <param name="maxCount">Max number of emails to fetch (null = no limit). Use for high-volume to avoid memory issues.</param>
    /// <param name="cancellationToken">Cancellation and timeout.</param>
    Task<List<FetchedEmail>> FetchUnreadEmailsAsync(int? maxCount = null, CancellationToken cancellationToken = default);
}

public class EmailReaderService : IEmailReaderService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailReaderService> _logger;
    private readonly TimeSpan _connectTimeout;

    public EmailReaderService(
        IOptions<EmailSettings> settings,
        IOptions<ProcessingEngineOptions> engineOptions,
        ILogger<EmailReaderService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        var timeoutSec = engineOptions?.Value?.ImapTimeoutSeconds ?? 30;
        _connectTimeout = TimeSpan.FromSeconds(Math.Max(5, Math.Min(timeoutSec, 120)));
    }

    public async Task<List<FetchedEmail>> FetchUnreadEmailsAsync(int? maxCount = null, CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            connectCts.CancelAfter(_connectTimeout);
            await client.ConnectAsync(_settings.ImapServer, _settings.Port, _settings.UseSsl, connectCts.Token);
            await client.AuthenticateAsync(_settings.Email, _settings.Password, connectCts.Token);
        }

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
        var total = uids.Count;

        if (total == 0)
        {
            await client.DisconnectAsync(true, cancellationToken);
            return new List<FetchedEmail>();
        }

        var toFetch = maxCount.HasValue && total > maxCount.Value
            ? uids.Take(maxCount.Value).ToList()
            : uids;

        var messages = new List<FetchedEmail>(toFetch.Count);

        foreach (var uid in toFetch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                messages.Add(new FetchedEmail { Uid = uid, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch message UID {Uid}, skipping", uid);
            }
        }

        if (maxCount.HasValue && total > maxCount.Value)
            _logger.LogInformation("Fetched {Count} of {Total} unread emails (batch limit)", messages.Count, total);
        else
            _logger.LogInformation("Fetched {Count} unread emails", messages.Count);

        await client.DisconnectAsync(true, cancellationToken);
        return messages;
    }
}
