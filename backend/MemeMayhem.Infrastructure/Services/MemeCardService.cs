using System.Text.Json;
using MemeMayhem.Core.Entities;
using MemeMayhem.Core.Enums;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemeMayhem.Infrastructure.Services;

public class MemeCardService : IMemeCardService
{
    private readonly HttpClient _http;
    private readonly MemeMayhemDbContext _db;
    private readonly ILogger<MemeCardService> _logger;

    public MemeCardService(
        HttpClient http,
        MemeMayhemDbContext db,
        ILogger<MemeCardService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task SyncImgflipDeckAsync()
    {
        try
        {
            _logger.LogInformation("Syncing Meme Cards from Imgflip...");

            var response = await _http.GetStringAsync("https://api.imgflip.com/get_memes");
            var parsed = JsonDocument.Parse(response);

            var isSuccess = parsed.RootElement.GetProperty("success").GetBoolean();
            if (!isSuccess)
            {
                _logger.LogWarning("Imgflip API returned success = false");
                return;
            }

            var memes = parsed
                .RootElement
                .GetProperty("data")
                .GetProperty("memes")
                .EnumerateArray();

            int added = 0;

            foreach (var meme in memes)
            {
                var externalId = meme.GetProperty("id").GetString() ?? string.Empty;

                bool exists = await _db.MemeCards
                    .AnyAsync(m => m.ExternalId == externalId);

                if (exists) continue;

                var memeUrl = meme.GetProperty("url").GetString() ?? string.Empty;
                var memeName = meme.GetProperty("name").GetString() ?? "Unknown Meme";

                if (string.IsNullOrEmpty(memeUrl)) continue;

                await _db.MemeCards.AddAsync(new MemeCard
                {
                    Id = Guid.NewGuid(),
                    ExternalId = externalId,
                    Label = memeName,
                    ImageUrl = memeUrl,
                    Source = CardSource.Imgflip,
                    CreatedAt = DateTime.UtcNow
                });

                added++;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Imgflip sync complete. Added {Count} new Memes.", added);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Imgflip Memes");
        }
    }
}
