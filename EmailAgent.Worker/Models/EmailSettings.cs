namespace EmailAgent.Worker.Models;

public class EmailSettings
{
    public string ImapServer { get; set; } = "";
    public int Port { get; set; }
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>IMAP folder for drafts (e.g. "[Gmail]/Drafts" for Gmail, "Drafts" for Outlook). If not set, auto-detected from ImapServer.</summary>
    public string? DraftsFolder { get; set; }
}