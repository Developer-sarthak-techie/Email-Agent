using EmailAgent.Worker.Models;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker.Services;

public interface IEmailMover
{
    Task MoveToLabelAsync(UniqueId uid, string label);
}
public class EmailMover:IEmailMover
{
    private readonly EmailSettings _settings;

    public EmailMover(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task MoveToLabelAsync(UniqueId uid, string label)
    {
        using var client = new ImapClient();

        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true);
        await client.AuthenticateAsync(_settings.Email, _settings.Password);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        // Get ALL folders recursively
        var root = client.GetFolder(client.PersonalNamespaces[0]);
        var allFolders = await root.GetSubfoldersAsync(false);

        var targetFolder = allFolders
            .FirstOrDefault(f => f.Name.Equals(label, StringComparison.OrdinalIgnoreCase));

        if (targetFolder == null)
        {
            throw new Exception($"Label '{label}' not found in mailbox.");
        }

        await inbox.MoveToAsync(uid, targetFolder);

        await client.DisconnectAsync(true);
    }
}