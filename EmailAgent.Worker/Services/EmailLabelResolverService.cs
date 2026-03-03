using EmailAgent.Worker.Models;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker.Services;

/// <summary>
/// Resolves intent enums to mailbox label names and ensures the label (folder) exists,
/// creating it when missing to avoid move failures.
/// </summary>
public interface IEmailLabelResolverService
{
    /// <summary>
    /// Returns the canonical label name for the given intent (e.g. "TransactionIssue", "RefundRequest").
    /// </summary>
    string GetLabelNameForIntent(FintechEmailIntent.FintechEmailIntentEnum intent);

    /// <summary>
    /// Ensures a folder with the given name exists under the mailbox root, creating it if missing.
    /// </summary>
    Task EnsureLabelExistsAsync(string labelName, CancellationToken cancellationToken = default);
}

public class EmailLabelResolverService : IEmailLabelResolverService
{
    private readonly EmailSettings _settings;
    private readonly IImapConnectionThrottle _throttle;

    public EmailLabelResolverService(IOptions<EmailSettings> settings, IImapConnectionThrottle throttle)
    {
        _settings = settings.Value;
        _throttle = throttle;
    }

    public string GetLabelNameForIntent(FintechEmailIntent.FintechEmailIntentEnum intent)
    {
        return intent.ToString();
    }

    public async Task EnsureLabelExistsAsync(string labelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(labelName))
            return;

        using (await _throttle.AcquireAsync(cancellationToken))
        {
            await EnsureLabelExistsCoreAsync(labelName, cancellationToken);
        }
    }

    private async Task EnsureLabelExistsCoreAsync(string labelName, CancellationToken cancellationToken)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true, cancellationToken);
        await client.AuthenticateAsync(_settings.Email, _settings.Password, cancellationToken);

        try
        {
            var root = client.GetFolder(client.PersonalNamespaces[0]);
            var subfolders = await root.GetSubfoldersAsync(false, cancellationToken);
            var exists = subfolders.Any(f => f.Name.Equals(labelName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
                await root.CreateAsync(labelName, true, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
