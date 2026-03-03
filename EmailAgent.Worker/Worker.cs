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
            var validator = scope.ServiceProvider.GetRequiredService<IEmailProcessingValidatorService>();

            var emails = await reader.FetchUnreadEmailsAsync();

            var labelService = scope.ServiceProvider.GetRequiredService<IEmailLabelService>();
            var labels = await labelService.GetAllLabelsAsync();
            foreach (var label in labels)
                _logger.LogInformation("Found Label: {Label}", label);

            foreach (var email in emails)
            {
                var result = await validator.ProcessAndValidateAsync(email, stoppingToken);

                _logger.LogInformation(
                    "Processed | Success: {Success} | Label: {Label} | Message: {Message}",
                    result.Success, result.Label ?? "(none)", result.Message ?? "");

                if (result.Intent.HasValue)
                    _logger.LogInformation(
                        "Intent: {Intent} | Score: {Score} | Confidence: {Confidence}%",
                        result.Intent, result.Score, result.Confidence);

                _logger.LogInformation("Message ID: {MessageId} | From: {From} | Subject: {Subject}",
                    email.Message.MessageId, email.Message.From, email.Message.Subject);
                _logger.LogInformation("======================================");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
