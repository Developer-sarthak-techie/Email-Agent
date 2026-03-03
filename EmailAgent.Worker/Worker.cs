using EmailAgent.Worker.Models;
using EmailAgent.Worker.Services;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ProcessingEngineOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<ProcessingEngineOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options?.Value ?? new ProcessingEngineOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var runStart = DateTime.UtcNow;
            int processed = 0, succeeded = 0, failed = 0;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reader = scope.ServiceProvider.GetRequiredService<IEmailReaderService>();
                var validator = scope.ServiceProvider.GetRequiredService<IEmailProcessingValidatorService>();

                var emails = await reader.FetchUnreadEmailsAsync(_options.BatchSize, stoppingToken);

                if (emails.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                // Optional: log label count only when batch is small (avoid noise for 10k+ runs)
                if (emails.Count <= 100)
                {
                    var labelService = scope.ServiceProvider.GetRequiredService<IEmailLabelService>();
                    var labels = await labelService.GetAllLabelsAsync();
                    _logger.LogInformation("Found {Count} labels", labels.Count);
                }

                var parallelism = Math.Max(1, Math.Min(_options.MaxDegreeOfParallelism, 50));
                var perEmailTimeout = TimeSpan.FromSeconds(Math.Max(10, Math.Min(_options.PerEmailTimeoutSeconds, 300)));

                await Parallel.ForEachAsync(
                    emails,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = parallelism,
                        CancellationToken = stoppingToken
                    },
                    async (email, ct) =>
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(perEmailTimeout);

                        try
                        {
                            var result = await validator.ProcessAndValidateAsync(email, cts.Token);
                            if (result.Success)
                                Interlocked.Increment(ref succeeded);
                            else
                                Interlocked.Increment(ref failed);
                        }
                        catch (OperationCanceledException)
                        {
                            Interlocked.Increment(ref failed);
                            _logger.LogWarning("Email processing timed out or was cancelled: MessageId {MessageId}", email.Message?.MessageId);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failed);
                            _logger.LogError(ex, "Unhandled error processing email: MessageId {MessageId}", email.Message?.MessageId);
                        }
                        finally
                        {
                            Interlocked.Increment(ref processed);
                        }
                    });

                var elapsed = DateTime.UtcNow - runStart;
                _logger.LogInformation(
                    "Batch complete | Processed: {Processed} | Success: {Succeeded} | Failed: {Failed} | Elapsed: {Elapsed:F1}s | Parallelism: {Parallelism}",
                    processed, succeeded, failed, elapsed.TotalSeconds, parallelism);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Run error in email processing loop");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
