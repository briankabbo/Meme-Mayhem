using MemeMayhem.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeMayhem.Infrastructure.Services;

public class StartupSyncService : IHostedService
{
    private readonly IMemeCardService _memeCardService;
    private readonly IGiphyService _giphyService;      // ← updated
    private readonly ILogger<StartupSyncService> _logger;

    public StartupSyncService(
        IMemeCardService memeCardService,
        IGiphyService giphyService,                    // ← updated
        ILogger<StartupSyncService> logger)
    {
        _memeCardService = memeCardService;
        _giphyService = giphyService;                  // ← updated
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running startup sync...");

        await _memeCardService.SyncImgflipDeckAsync();
        await _giphyService.SyncReactionGifsAsync();   // ← updated

        _logger.LogInformation("Startup sync complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}