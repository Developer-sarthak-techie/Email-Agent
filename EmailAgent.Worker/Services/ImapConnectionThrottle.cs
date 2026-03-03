using EmailAgent.Worker.Models;
using Microsoft.Extensions.Options;

namespace EmailAgent.Worker.Services;

/// <summary>
/// Limits simultaneous IMAP connections so we never exceed the provider limit (e.g. Outlook/Gmail "Too many simultaneous connections").
/// </summary>
public interface IImapConnectionThrottle
{
    /// <summary>Wait until a connection slot is available. Dispose of the returned object (or call Release) when done.</summary>
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}

public sealed class ImapConnectionThrottle : IImapConnectionThrottle
{
    private readonly SemaphoreSlim _semaphore;

    public ImapConnectionThrottle(IOptions<ProcessingEngineOptions> options)
    {
        var max = options?.Value?.MaxConcurrentImapConnections ?? 5;
        _semaphore = new SemaphoreSlim(Math.Max(1, Math.Min(max, 15)), Math.Max(1, Math.Min(max, 15)));
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        private bool _released;

        public Releaser(SemaphoreSlim sem) => _sem = sem;

        public void Dispose()
        {
            if (_released) return;
            _sem.Release();
            _released = true;
        }
    }
}
