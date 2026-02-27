using EmailAgent.Worker.Services;

namespace EmailAgent.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
  

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
       
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var reader = scope.ServiceProvider.GetRequiredService<IEmailReaderService>();
            var mover = scope.ServiceProvider.GetRequiredService<IEmailMover>();
            var scorer = scope.ServiceProvider.GetRequiredService<IntentScorer>();
            var draftService = scope.ServiceProvider.GetRequiredService<IEmailDraftService>();
            var fintechDraftGenerator = scope.ServiceProvider.GetRequiredService<FintechDraftGenerator>();
            var detectIntent = scope.ServiceProvider.GetRequiredService<FintechIntentEngine>();
            
            var emails = await reader.FetchUnreadEmailsAsync();

            // using var scope = _scopeFactory.CreateScope();
            var labelService = scope.ServiceProvider.GetRequiredService<IEmailLabelService>();

            var labels = await labelService.GetAllLabelsAsync();

            foreach (var label in labels)
            {
                _logger.LogInformation("Found Label: {Label}", label);
            }

            foreach (var email in emails)
            {
                var subject = email.Message.Subject ?? "";
                var body = email.Message.TextBody ?? "";

                
                // 🔥 PRIORITY RULE 1 - JIRA DOMAIN
                if (email.Message.From.ToString().Contains("@taiservices.atlassian.net", StringComparison.OrdinalIgnoreCase))
                {
                    await mover.MoveToLabelAsync(email.Uid, "Jira");
                    _logger.LogInformation("Moved to Jira (sender rule matched)");
                    continue; // Skip scoring
                } 
                // 🔥 PRIORITY RULE 2 - Aws DOMAIN
                if (email.Message.From.ToString().Contains("no-reply@sns.amazonaws.com", StringComparison.OrdinalIgnoreCase))
                {
                    await mover.MoveToLabelAsync(email.Uid, "AWS");
                    _logger.LogInformation("Moved to AWS (sender rule matched)");
                    continue; // Skip scoring
                }

                // 🔥 FALLBACK TO SCORING ENGINE
                var (label, score) = scorer.Calculate(subject ?? "", body ?? "");

                _logger.LogInformation("Detected Label: {Label} | Score: {Score}", label, score);
                var intentResponse= detectIntent.Detect(subject ?? "", body ?? "");
                
                // for creating drafts
                
                if (score >= 70)
                {
                    var senderName = email.Message.From.Mailboxes.FirstOrDefault()?.Name ?? "Customer";

                    var draft = fintechDraftGenerator.Generate(intentResponse.Intent, senderName);

                    await draftService.CreateDraftReplyAsync(email.Message, draft);

                    _logger.LogInformation("Draft created successfully.");
                }

                if (score >= 70)
                {
                    await mover.MoveToLabelAsync(email.Uid, label);
                    _logger.LogInformation("Moving email to label: {Label}", label);

                    // We'll implement actual move next
                }
                else
                {
                    _logger.LogInformation("Score below threshold. Keeping in Inbox.");
                }
                _logger.LogInformation("======================================");
                _logger.LogInformation("Message ID: {MessageId}", email.Message.MessageId);
                _logger.LogInformation("From: {From}", email.Message.From);
                _logger.LogInformation("Date: {Date}", email.Message.Date);
                _logger.LogInformation("Subject: {Subject}", email.Message.Subject);
                _logger.LogInformation("Body Preview: {Body}",
                    email.Message.TextBody?.Length > 200
                        ? email.Message.TextBody.Substring(0, 200)
                        : email.Message.TextBody);
                _logger.LogInformation("======================================");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}