using MemeMayhem.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeMayhem.Infrastructure.Services;

public class StartupSyncService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupSyncService> _logger;

    public StartupSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<StartupSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running startup sync...");

        // Create a scope so we can resolve scoped services
        using var scope = _scopeFactory.CreateScope();

        var memeCardService = scope.ServiceProvider
            .GetRequiredService<IMemeCardService>();

        var giphyService = scope.ServiceProvider
            .GetRequiredService<IGiphyService>();

        await memeCardService.SyncImgflipDeckAsync();
        await giphyService.SyncReactionGifsAsync();

        _logger.LogInformation("Startup sync complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}