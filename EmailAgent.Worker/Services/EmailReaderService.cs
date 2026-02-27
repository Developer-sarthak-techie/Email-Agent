using MimeKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using Microsoft.Extensions.Options;
using EmailAgent.Worker.Models;


namespace EmailAgent.Worker.Services;

public interface IEmailReaderService
{
    Task<List<FetchedEmail>> FetchUnreadEmailsAsync();
}
public class EmailReaderService:IEmailReaderService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<Worker> _logger;
    public EmailReaderService(IOptions<EmailSettings> settings , ILogger<Worker> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }
    
    public async Task<List<FetchedEmail>> FetchUnreadEmailsAsync()
    {
        using var client = new ImapClient();

        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true);
        await client.AuthenticateAsync(_settings.Email, _settings.Password);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        var uids = await inbox.SearchAsync(SearchQuery.NotSeen);

        var messages = new List<FetchedEmail>();

        foreach (var uid in uids)
        {
            var message = await inbox.GetMessageAsync(uid);

            messages.Add(new FetchedEmail
            {
                Uid = uid,
                Message = message
            });
        }

        await client.DisconnectAsync(true);

        return messages;
    }
    
    
    // public async Task<List<MimeMessage>> FetchUnreadEmailsAsync()
    // {
    //     using var client = new ImapClient();
    //
    //     await client.ConnectAsync(_settings.ImapServer, _settings.Port, true);
    //     await client.AuthenticateAsync(_settings.Email, _settings.Password);
    //
    //     var inbox = client.Inbox;
    //     await inbox.OpenAsync(FolderAccess.ReadWrite);
    //
    //     var uids = await inbox.SearchAsync(SearchQuery.NotSeen);
    //
    //     var messages = new List<MimeMessage>();
    //
    //     _logger.LogInformation("Unread emails count: {Count}", uids.Count);
    //     foreach (var uid in uids)
    //     {
    //         var message = await inbox.GetMessageAsync(uid);
    //         messages.Add(message);
    //         var sender = message.From;
    //         _logger.LogInformation("Read email: {Email}", sender);
    //         // Mark as seen
    //       //  await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
    //     }
    //
    //     await client.DisconnectAsync(true);
    //
    //     return messages;
    // }
    //
}