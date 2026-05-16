// GiphyService.cs
using System.Text.Json;
using MemeMayhem.Core.Entities;
using MemeMayhem.Core.Enums;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemeMayhem.Infrastructure.Services;

public class GiphyService : IGiphyService
{
    private readonly HttpClient _http;
    private readonly MemeMayhemDbContext _db;
    private readonly ILogger<GiphyService> _logger;
    private readonly string _apiKey;
    private readonly Random _random = new();

    private const string GIPHY_BASE = "https://api.giphy.com/v1/gifs";

    private static readonly Dictionary<VoteType, string[]> _queries = new()
    {
        [VoteType.Haha] = new[]
        {
            "haha funny",
            "laughing reaction",
            "lol meme",
            "that's hilarious",
            "laughing cat"
        },
        [VoteType.Lmao] = new[]
        {
            "dead laughing",
            "im deceased funny",
            "rolling floor laughing",
            "this sent me",
            "crying laughing"
        },
        [VoteType.Meh] = new[]
        {
            "meh reaction",
            "whatever shrug",
            "unimpressed reaction",
            "boring reaction",
            "i dont care"
        }
    };

    public GiphyService(
        HttpClient http,
        MemeMayhemDbContext db,
        IConfiguration config,
        ILogger<GiphyService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
        _apiKey = config["Giphy:ApiKey"]
            ?? throw new InvalidOperationException(
                "Giphy API key not found");
    }

    public async Task SyncReactionGifsAsync()
    {
        try
        {
            _logger.LogInformation("Syncing Giphy reaction GIFs...");

            int added = 0;

            foreach (var (voteType, queries) in _queries)
            {
                foreach (var query in queries)
                {
                    var url = $"{GIPHY_BASE}/search" +
                              $"?api_key={_apiKey}" +
                              $"&q={Uri.EscapeDataString(query)}" +
                              $"&limit=5" +
                              $"&rating=pg-13" +
                              $"&lang=en";

                    var response = await _http.GetStringAsync(url);
                    var parsed = JsonDocument.Parse(response);

                    var results = parsed
                        .RootElement
                        .GetProperty("data")
                        .EnumerateArray();

                    foreach (var gif in results)
                    {
                        var giphyId = gif.GetProperty("id")
                            .GetString() ?? string.Empty;

                        // Skip if already in DB
                        bool exists = await _db.ReactionGifs
                            .AnyAsync(g => g.TenorId == giphyId);

                        if (exists) continue;

                        // Get GIF URL
                        var gifUrl = gif
                            .GetProperty("images")
                            .GetProperty("fixed_height")
                            .GetProperty("url")
                            .GetString() ?? string.Empty;

                        if (string.IsNullOrEmpty(gifUrl)) continue;

                        await _db.ReactionGifs.AddAsync(new ReactionGif
                        {
                            Id = Guid.NewGuid(),
                            VoteType = voteType,
                            GifUrl = gifUrl,
                            TenorId = giphyId, // reusing field for Giphy ID
                            CreatedAt = DateTime.UtcNow
                        });

                        added++;
                    }

                    // Small delay to be nice to the API
                    await Task.Delay(200);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Giphy sync complete. Added {Count} new GIFs.", added);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Giphy GIFs");
        }
    }

    public async Task<ReactionGif> GetRandomGifAsync(VoteType voteType)
    {
        var gifs = await GetGifsByVoteTypeAsync(voteType);

        if (gifs.Count == 0)
            throw new InvalidOperationException(
                $"No GIFs found for vote type {voteType}");

        return gifs[_random.Next(gifs.Count)];
    }

    public async Task<List<ReactionGif>> GetGifsByVoteTypeAsync(VoteType voteType)
    {
        return await _db.ReactionGifs
            .Where(g => g.VoteType == voteType)
            .ToListAsync();
    }
}