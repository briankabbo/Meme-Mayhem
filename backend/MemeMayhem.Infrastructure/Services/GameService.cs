// GameService.cs
using System.Text.Json;
using MemeMayhem.Core.DTOs;
using MemeMayhem.Core.Entities;
using MemeMayhem.Core.Enums;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MemeMayhem.Infrastructure.Services;

public class GameService : IGameService
{
    private readonly MemeMayhemDbContext _db;
    private readonly IAIPromptService _aiPromptService;

    public GameService(MemeMayhemDbContext db, IAIPromptService aiPromptService)
    {
        _db = db;
        _aiPromptService = aiPromptService;
    }

    public async Task<RoundDto> StartGameAsync(Guid roomId)
    {
        var room = await _db.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId)
            ?? throw new InvalidOperationException("Room not found");

        room.Status = RoomStatus.Active;
        room.CurrentRound = 1;
        await _db.SaveChangesAsync();

        await DealCardsAsync(roomId);
        return await StartRoundInternalAsync(room);
    }

    public async Task<RoundDto> StartNextRoundAsync(Guid roomId)
    {
        var room = await _db.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId)
            ?? throw new InvalidOperationException("Room not found");

        room.CurrentRound++;

        // Game over check
        if (room.CurrentRound > room.TotalRounds)
            throw new InvalidOperationException("Game already completed");

        await _db.SaveChangesAsync();
        return await StartRoundInternalAsync(room);
    }

    private async Task<RoundDto> StartRoundInternalAsync(Room room)
    {
        // Get active players only (not spectators)
        var activePlayers = room.Players
            .Where(p => !p.IsSpectator && p.IsConnected)
            .ToList();

        // Randomize turn order
        var turnOrder = activePlayers
            .Select(p => p.Id)
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        // Get previously used prompts to avoid repetition
        var usedPrompts = await _db.Rounds
            .Where(r => r.RoomId == room.Id)
            .Select(r => r.PromptText)
            .ToListAsync();

        var prompt = await _aiPromptService.GeneratePromptAsync(room.Theme, usedPrompts);

        var round = new Round
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            RoundNumber = room.CurrentRound,
            PromptText = prompt,
            Status = RoundStatus.CardPicking,
            TurnOrder = JsonSerializer.Serialize(turnOrder),
            CurrentTurnIndex = 0,
            StartedAt = DateTime.UtcNow
        };

        await _db.Rounds.AddAsync(round);
        await _db.SaveChangesAsync();

        return MapToRoundDto(round, activePlayers);
    }

    public async Task<CardPlayDto> SubmitCardAsync(
        Guid roundId, Guid playerId, Guid cardId)
    {
        var round = await _db.Rounds.FindAsync(roundId)
            ?? throw new InvalidOperationException("Round not found");

        var turnOrder = JsonSerializer
            .Deserialize<List<Guid>>(round.TurnOrder) ?? new();

        // Enforce turn order
        if (turnOrder[round.CurrentTurnIndex] != playerId)
            throw new InvalidOperationException("Not your turn");

        // Mark card as played
        var playerCard = await _db.PlayerCards
            .FirstOrDefaultAsync(pc =>
                pc.PlayerId == playerId &&
                pc.MemeCardId == cardId &&
                !pc.IsPlayed)
            ?? throw new InvalidOperationException("Card not in hand");

        playerCard.IsPlayed = true;

        var cardPlay = new CardPlay
        {
            Id = Guid.NewGuid(),
            RoundId = roundId,
            PlayerId = playerId,
            MemeCardId = cardId,
            TurnIndex = round.CurrentTurnIndex,
            PlayedAt = DateTime.UtcNow
        };

        await _db.CardPlays.AddAsync(cardPlay);
        await _db.SaveChangesAsync();

        // Load card details for DTO
        var card = await _db.MemeCards.FindAsync(cardId);
        var player = await _db.Players.FindAsync(playerId);

        return new CardPlayDto
        {
            Id = cardPlay.Id,
            PlayerId = playerId,
            PlayerName = player!.Nickname,
            TurnIndex = cardPlay.TurnIndex,
            Card = new MemeCardDto
            {
                Id = card!.Id,
                Label = card.Label,
                ImageUrl = card.ImageUrl
            },
            Votes = new List<VoteDto>()
        };
    }

    public async Task<VoteDto> SubmitVoteAsync(
        Guid cardPlayId, Guid voterId, string voteType)
    {
        var cardPlay = await _db.CardPlays.FindAsync(cardPlayId)
            ?? throw new InvalidOperationException("Card play not found");

        // Can't vote on own card
        if (cardPlay.PlayerId == voterId)
            throw new InvalidOperationException("Cannot vote on your own card");

        // Can't double vote
        var existingVote = await _db.Votes
            .AnyAsync(v => v.CardPlayId == cardPlayId && v.VoterId == voterId);

        if (existingVote)
            throw new InvalidOperationException("Already voted on this card");

        var parsedVoteType = Enum.Parse<VoteType>(voteType, ignoreCase: true);

        int points = parsedVoteType switch
        {
            VoteType.Haha => 1,
            VoteType.Lmao => 5,
            VoteType.Meh  => 0,
            _ => 0
        };

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            CardPlayId = cardPlayId,
            VoterId = voterId,
            VoteType = parsedVoteType,
            Points = points,
            VotedAt = DateTime.UtcNow
        };

        await _db.Votes.AddAsync(vote);
        await _db.SaveChangesAsync();

        var voter = await _db.Players.FindAsync(voterId);

        return new VoteDto
        {
            VoterId = voterId,
            VoterName = voter!.Nickname,
            VoteType = voteType,
            Points = points
        };
    }

    public async Task<bool> IsTurnCompleteAsync(Guid cardPlayId)
    {
        var cardPlay = await _db.CardPlays
            .Include(cp => cp.Round)
                .ThenInclude(r => r.Room)
                    .ThenInclude(room => room.Players)
            .FirstOrDefaultAsync(cp => cp.Id == cardPlayId)
            ?? throw new InvalidOperationException("Card play not found");

        var activePlayers = cardPlay.Round.Room.Players
            .Count(p => !p.IsSpectator && p.IsConnected);

        // Everyone except the card owner votes
        int votesNeeded = activePlayers - 1;

        int votesCast = await _db.Votes
            .CountAsync(v => v.CardPlayId == cardPlayId);

        return votesCast >= votesNeeded;
    }

    public async Task<bool> IsRoundCompleteAsync(Guid roundId)
    {
        var round = await _db.Rounds.FindAsync(roundId)
            ?? throw new InvalidOperationException("Round not found");

        var turnOrder = JsonSerializer
            .Deserialize<List<Guid>>(round.TurnOrder) ?? new();

        // All players have had their turn
        return round.CurrentTurnIndex >= turnOrder.Count;
    }

    public async Task SkipTurnAsync(Guid roundId)
    {
        var round = await _db.Rounds.FindAsync(roundId)
            ?? throw new InvalidOperationException("Round not found");

        round.CurrentTurnIndex++;
        await _db.SaveChangesAsync();
    }

    public async Task<RoundResultDto> EndRoundAsync(Guid roundId)
    {
        var round = await _db.Rounds
            .Include(r => r.CardPlays)
                .ThenInclude(cp => cp.Votes)
            .Include(r => r.Room)
                .ThenInclude(room => room.Players)
            .FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new InvalidOperationException("Round not found");

        round.Status = RoundStatus.Completed;
        round.EndedAt = DateTime.UtcNow;

        var scores = new List<PlayerScoreDto>();

        // Calculate points per player from votes received
        foreach (var cardPlay in round.CardPlays)
        {
            var pointsEarned = cardPlay.Votes.Sum(v => v.Points);

            // Update player total
            var player = round.Room.Players
                .First(p => p.Id == cardPlay.PlayerId);

            player.TotalScore += pointsEarned;

            var roundScore = new RoundScore
            {
                Id = Guid.NewGuid(),
                RoundId = roundId,
                PlayerId = cardPlay.PlayerId,
                PointsEarned = pointsEarned,
                RunningTotal = player.TotalScore
            };

            await _db.RoundScores.AddAsync(roundScore);

            scores.Add(new PlayerScoreDto
            {
                PlayerId = cardPlay.PlayerId,
                PlayerName = player.Nickname,
                PointsEarned = pointsEarned,
                RunningTotal = player.TotalScore
            });
        }

        await _db.SaveChangesAsync();

        var rankedScores = scores
            .OrderByDescending(s => s.RunningTotal)
            .Select((s, index) =>
            {
                s.Rank = index + 1;
                return s;
            })
            .ToList();

        return new RoundResultDto
        {
            RoundId = roundId,
            RoundNumber = round.RoundNumber,
            TotalRounds = round.Room.TotalRounds,
            IsGameOver = round.RoundNumber >= round.Room.TotalRounds,
            Scores = rankedScores
        };
    }

    public async Task DealCardsAsync(Guid roomId)
    {
        var room = await _db.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId)
            ?? throw new InvalidOperationException("Room not found");

        var activePlayers = room.Players
            .Where(p => !p.IsSpectator)
            .ToList();

        // Get shuffled deck
        var deck = await GetShuffledDeckAsync(roomId, activePlayers.Count * 8);

        int cardIndex = 0;

        foreach (var player in activePlayers)
        {
            for (int i = 0; i < 5 && cardIndex < deck.Count; i++, cardIndex++)
            {
                await _db.PlayerCards.AddAsync(new PlayerCard
                {
                    Id = Guid.NewGuid(),
                    PlayerId = player.Id,
                    MemeCardId = deck[cardIndex].Id,
                    RoomId = roomId,
                    IsPlayed = false
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task DrawCardAsync(Guid playerId, Guid roomId)
    {
        // Get cards not yet assigned to this player in this room
        var assignedCardIds = await _db.PlayerCards
            .Where(pc => pc.RoomId == roomId)
            .Select(pc => pc.MemeCardId)
            .ToListAsync();

        var availableCard = await _db.MemeCards
            .Where(mc => !assignedCardIds.Contains(mc.Id))
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefaultAsync();

        // Deck exhausted → reshuffle played cards
        if (availableCard == null)
        {
            var playedCard = await _db.PlayerCards
                .Where(pc => pc.RoomId == roomId && pc.IsPlayed)
                .OrderBy(_ => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (playedCard == null) return;

            playedCard.PlayerId = playerId;
            playedCard.IsPlayed = false;
            playedCard.DealtAt = DateTime.UtcNow;
        }
        else
        {
            await _db.PlayerCards.AddAsync(new PlayerCard
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                MemeCardId = availableCard.Id,
                RoomId = roomId,
                IsPlayed = false
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<MemeCardDto>> GetPlayerHandAsync(Guid playerId)
    {
        return await _db.PlayerCards
            .Where(pc => pc.PlayerId == playerId && !pc.IsPlayed)
            .Include(pc => pc.MemeCard)
            .Select(pc => new MemeCardDto
            {
                Id = pc.MemeCard.Id,
                Label = pc.MemeCard.Label,
                ImageUrl = pc.MemeCard.ImageUrl
            })
            .ToListAsync();
    }

    public async Task<CardPlayWithRoundDto?> GetCardPlayWithRoundAsync(Guid cardPlayId)
    {
        var cardPlay = await _db.CardPlays
            .Include(cp => cp.Round)
                .ThenInclude(r => r.Room)
                    .ThenInclude(room => room.Players)
            .FirstOrDefaultAsync(cp => cp.Id == cardPlayId);

        if (cardPlay == null) return null;

        var turnOrder = JsonSerializer
            .Deserialize<List<Guid>>(cardPlay.Round.TurnOrder) ?? new();

        return new CardPlayWithRoundDto
        {
            Id = cardPlay.Id,
            PlayerId = cardPlay.PlayerId,
            Round = new RoundDto
            {
                Id = cardPlay.Round.Id,
                RoundNumber = cardPlay.Round.RoundNumber,
                PromptText = cardPlay.Round.PromptText,
                Status = cardPlay.Round.Status.ToString(),
                CurrentPlayerId = turnOrder.ElementAtOrDefault(
                    cardPlay.Round.CurrentTurnIndex),
                CurrentTurnIndex = cardPlay.Round.CurrentTurnIndex,
                TotalTurns = turnOrder.Count,
                CardPlays = new List<CardPlayDto>()
            }
        };
    }

    public async Task<RoundDto?> GetRoundDtoAsync(Guid roundId)
    {
        var round = await _db.Rounds
            .Include(r => r.Room)
                .ThenInclude(room => room.Players)
            .FirstOrDefaultAsync(r => r.Id == roundId);

        if (round == null) return null;

        var activePlayers = round.Room.Players
            .Where(p => !p.IsSpectator && p.IsConnected)
            .ToList();

        return MapToRoundDto(round, activePlayers);
    }

    private async Task<List<MemeCard>> GetShuffledDeckAsync(
        Guid roomId, int count)
    {
        return await _db.MemeCards
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
    }

    private RoundDto MapToRoundDto(Round round, List<Player> activePlayers)
    {
        var turnOrder = JsonSerializer
            .Deserialize<List<Guid>>(round.TurnOrder) ?? new();

        return new RoundDto
        {
            Id = round.Id,
            RoundNumber = round.RoundNumber,
            PromptText = round.PromptText,
            Status = round.Status.ToString(),
            CurrentPlayerId = turnOrder.FirstOrDefault(),
            CurrentTurnIndex = round.CurrentTurnIndex,
            TotalTurns = turnOrder.Count,
            CardPlays = new List<CardPlayDto>()
        };
    }
}