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

            var emails = await reader.FetchUnreadEmailsAsync();

            
                foreach (var email in emails)
                {
                    _logger.LogInformation("======================================");
                    _logger.LogInformation("Message ID: {MessageId}", email.MessageId);
                    _logger.LogInformation("From: {From}", email.From);
                    _logger.LogInformation("Date: {Date}", email.Date);
                    _logger.LogInformation("Subject: {Subject}", email.Subject);
                    _logger.LogInformation("Body Preview: {Body}",
                        email.TextBody?.Length > 200
                            ? email.TextBody.Substring(0, 200)
                            : email.TextBody);
                    _logger.LogInformation("======================================");
                }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
