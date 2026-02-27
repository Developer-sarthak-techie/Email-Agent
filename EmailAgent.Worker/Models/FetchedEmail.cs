using MailKit;
using MimeKit;

namespace EmailAgent.Worker.Models;

public class FetchedEmail
{
    public UniqueId Uid { get; set; }
    public MimeMessage Message { get; set; }
}