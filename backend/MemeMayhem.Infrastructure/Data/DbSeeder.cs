using System.Text.Json;
using MemeMayhem.Core.Entities;
using MemeMayhem.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemeMayhem.Infrastructure.Data;

public class DbSeeder
{
    private readonly MemeMayhemDbContext _db;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(MemeMayhemDbContext db, ILogger<DbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedMemeCardsAsync()
    {
        try
        {
            var seedPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data", "Seeds", "meme_cards.json"
            );

            if (!File.Exists(seedPath))
            {
                _logger.LogWarning("Seed file not found at {Path}", seedPath);
                return;
            }

            var json = await File.ReadAllTextAsync(seedPath);
            var seedCards = JsonSerializer.Deserialize<List<SeedCard>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (seedCards == null || seedCards.Count == 0)
            {
                _logger.LogWarning("No cards in seed file");
                return;
            }

            var existingIds = await _db.MemeCards
                .Select(m => m.ExternalId)
                .ToHashSetAsync();

            int added = 0;

            foreach (var card in seedCards)
            {
                if (existingIds.Contains(card.Id)) continue;

                await _db.MemeCards.AddAsync(new MemeCard
                {
                    Id = Guid.NewGuid(),
                    ExternalId = card.Id,
                    Label = card.Label,
                    StoragePath = card.StoragePath,
                    Tags = JsonSerializer.Serialize(card.Tags),
                    Source = CardSource.Custom,
                    CreatedAt = DateTime.UtcNow
                });

                added++;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} meme cards", added);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed meme cards");
        }
    }

    private class SeedCard
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}