namespace EmailAgent.Worker.Models;

public class EmailSettings
{
    /// <summary>
    /// Supported values: Gmail, Zoho. Defaults to Gmail if not configured.
    /// </summary>
    public string Provider { get; set; } = "Gmail";

    public string ImapServer { get; set; } = "";
    public int Port { get; set; }
    public bool UseSsl { get; set; } = true;
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>IMAP folder for drafts (e.g. "[Gmail]/Drafts" for Gmail, "Drafts" for Outlook). If not set, auto-detected from ImapServer.</summary>
    public string? DraftsFolder { get; set; }

    public void ApplyProviderDefaults()
    {
        var provider = (Provider ?? string.Empty).Trim().ToLowerInvariant();

        if (provider == "zoho")
        {
            if (string.IsNullOrWhiteSpace(ImapServer))
                ImapServer = "imap.zoho.com";
            if (Port <= 0)
                Port = 993;
            if (string.IsNullOrWhiteSpace(DraftsFolder))
                DraftsFolder = "Drafts";
            return;
        }

        // Default to Gmail behavior for backward compatibility.
        if (string.IsNullOrWhiteSpace(ImapServer))
            ImapServer = "imap.gmail.com";
        if (Port <= 0)
            Port = 993;
        if (string.IsNullOrWhiteSpace(DraftsFolder))
            DraftsFolder = "[Gmail]/Drafts";
    }
}