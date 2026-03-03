using EmailAgent.Worker.Models;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailAgent.Worker.Services;

public interface IEmailDraftService
{
    Task CreateDraftReplyAsync(MimeMessage originalMessage, string draftBody, IReadOnlyList<string>? ccAddresses = null);
}

public class EmailDraftService:IEmailDraftService
{
    private readonly EmailSettings _settings;

    public EmailDraftService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task CreateDraftReplyAsync(MimeMessage originalMessage, string draftBody, IReadOnlyList<string>? ccAddresses = null)
    {
        using var client = new ImapClient();

        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true);
        await client.AuthenticateAsync(_settings.Email, _settings.Password);

        // 🔹 Create reply
        var reply = new MimeMessage();

        reply.From.Add(MailboxAddress.Parse(_settings.Email));
        reply.To.AddRange(originalMessage.ReplyTo.Any()
            ? originalMessage.ReplyTo
            : originalMessage.From);

        if (ccAddresses?.Count > 0)
        {
            foreach (var cc in ccAddresses)
                reply.Cc.Add(MailboxAddress.Parse(cc));
        }

        var subj = originalMessage.Subject ?? "";
        reply.Subject = subj.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? subj : "Re: " + subj;

        // Maintain thread
        reply.InReplyTo = originalMessage.MessageId;
        reply.References.Add(originalMessage.MessageId);

        reply.Body = new TextPart("plain")
        {
            Text = draftBody
        };

        // 🔹 Append to Gmail Drafts folder
        var drafts = await client.GetFolderAsync("[Gmail]/Drafts");
        await drafts.OpenAsync(FolderAccess.ReadWrite);

        await drafts.AppendAsync(reply, MessageFlags.Draft);

        await client.DisconnectAsync(true);
    }
}