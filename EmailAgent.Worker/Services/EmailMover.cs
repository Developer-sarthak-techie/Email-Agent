using EmailAgent.Worker.Models;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker.Services;

public interface IEmailMover
{
    Task MoveToLabelAsync(UniqueId uid, string label, CancellationToken cancellationToken = default);
}

public class EmailMover : IEmailMover
{
    private readonly EmailSettings _settings;

    public EmailMover(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task MoveToLabelAsync(UniqueId uid, string label, CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true, cancellationToken);
        await client.AuthenticateAsync(_settings.Email, _settings.Password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        var root = client.GetFolder(client.PersonalNamespaces[0]);
        var allFolders = await root.GetSubfoldersAsync(false, cancellationToken);
        var targetFolder = allFolders
            .FirstOrDefault(f => f.Name.Equals(label, StringComparison.OrdinalIgnoreCase));

        if (targetFolder == null)
            throw new InvalidOperationException($"Label '{label}' not found in mailbox.");

        await inbox.MoveToAsync(uid, targetFolder, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}