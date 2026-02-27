using EmailAgent.Worker.Models;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker.Services;

public interface IEmailLabelService
{
    Task<List<string>> GetAllLabelsAsync();
}

public class EmailLabelService:IEmailLabelService
{
    private readonly EmailSettings _settings;

    public EmailLabelService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<List<string>> GetAllLabelsAsync()
    {
        using var client = new ImapClient();

        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true);
        await client.AuthenticateAsync(_settings.Email, _settings.Password);

        var folders = await client.GetFoldersAsync(client.PersonalNamespaces[0]);

        var labels = folders.Select(f => f.Name).ToList();

        await client.DisconnectAsync(true);

        return labels;
    }
    
}