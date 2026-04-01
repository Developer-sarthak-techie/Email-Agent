# Email-Agent

POC for an email automation worker.

## IMAP Provider Configuration

The worker reads IMAP settings from `EmailAgent.Worker/appsettings.json` under `EmailSettings`.

Supported providers:
- `Gmail`
- `Zoho`

Example:

```json
"EmailSettings": {
  "Provider": "Zoho",
  "ImapServer": "imap.zoho.com",
  "Port": 993,
  "UseSsl": true,
  "Email": "yourname@yourdomain.com",
  "Password": "your-app-password",
  "DraftsFolder": "Drafts"
}
```

If `ImapServer`, `Port`, or `DraftsFolder` are omitted, defaults are applied from `Provider`:
- `Gmail` => `imap.gmail.com`, `993`, `[Gmail]/Drafts`
- `Zoho` => `imap.zoho.com`, `993`, `Drafts`

## How to connect Zoho mailbox

1. In Zoho Mail account settings, enable IMAP access.
2. If MFA is enabled (recommended), create an app-specific password in Zoho Security settings.
3. Put your mailbox email in `EmailSettings:Email`.
4. Put the app password in `EmailSettings:Password`.
5. Set `EmailSettings:Provider` to `Zoho`.
6. Run worker: `dotnet run --project EmailAgent.Worker`.

Tip: avoid storing real passwords in committed `appsettings.json`; use user-secrets or environment variables for production.
