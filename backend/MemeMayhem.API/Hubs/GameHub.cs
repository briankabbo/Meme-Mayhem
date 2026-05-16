using MemeMayhem.Core.Entities;
using MemeMayhem.Core.DTOs;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Core.Enums;
using MemeMayhem.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;

namespace MemeMayhem.API.Hubs;

public class GameHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly IGameService _gameService;
    private static readonly Dictionary<Guid, CancellationTokenSource> _turnTimers = new();
    private static readonly Dictionary<Guid, CancellationTokenSource> _disconnectTimers = new();

    public GameHub(IRoomService roomService, IGameService gameService)
    {
        _roomService = roomService;
        _gameService = gameService;
    }

    // ─── CONNECTION ──────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var player = await _roomService
            .GetPlayerByConnectionAsync(Context.ConnectionId);

        if (player == null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        await _roomService.UpdateConnectionAsync(
            player.Id, Context.ConnectionId, isConnected: false);

        await Clients.Group(player.RoomId.ToString())
            .SendAsync("PlayerDisconnected", new
            {
                PlayerId = player.Id,
                Nickname = player.Nickname
            });

        // If host disconnects → promote next player
        if (player.IsHost)
            await PromoteHostAsync(player.RoomId);

        // 30s grace period before auto-skip
        var cts = new CancellationTokenSource();
        _disconnectTimers[player.Id] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

                // Still disconnected after 30s → notify room
                await Clients.Group(player.RoomId.ToString())
                    .SendAsync("PlayerTimedOut", new
                    {
                        PlayerId = player.Id,
                        Nickname = player.Nickname
                    });
            }
            catch (TaskCanceledException) { }
        }, cts.Token);

        await base.OnDisconnectedAsync(exception);
    }

    // ─── ROOM ────────────────────────────────────────────────

    public async Task CreateRoom(string nickname, string theme, int totalRounds)
    {
        try
        {
            var room = await _roomService
                .CreateRoomAsync(nickname, theme, totalRounds);

            var player = room.Players.First();

            // Update connection ID
            await _roomService.UpdateConnectionAsync(
                player.Id, Context.ConnectionId, isConnected: true);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

            await Clients.Caller.SendAsync("RoomCreated", new
            {
                RoomId = room.Id,
                Code = room.Code,
                PlayerId = player.Id,
                IsHost = true
            });
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task JoinRoom(string code, string nickname)
    {
        try
        {
            var room = await _roomService.GetRoomByCodeAsync(code);

            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            // Game already started → spectator
            if (room.Status != RoomStatus.Lobby)
            {
                var spectator = await _roomService
                    .JoinAsSpectatorAsync(code, nickname);

                await _roomService.UpdateConnectionAsync(
                    spectator.Id, Context.ConnectionId, isConnected: true);

                await Groups.AddToGroupAsync(
                    Context.ConnectionId, room.Id.ToString());

                await Clients.Caller.SendAsync("JoinedAsSpectator", new
                {
                    RoomId = room.Id,
                    PlayerId = spectator.Id
                });

                await Clients.Group(room.Id.ToString())
                    .SendAsync("SpectatorJoined", new
                    {
                        Nickname = nickname
                    });
                return;
            }

            // Join as player
            var (updatedRoom, player) = await _roomService
                .JoinRoomAsync(code, nickname);

            await _roomService.UpdateConnectionAsync(
                player.Id, Context.ConnectionId, isConnected: true);

            await Groups.AddToGroupAsync(
                Context.ConnectionId, updatedRoom.Id.ToString());

            await Clients.Caller.SendAsync("RoomJoined", new
            {
                RoomId = updatedRoom.Id,
                PlayerId = player.Id,
                IsHost = false
            });

            await Clients.Group(updatedRoom.Id.ToString())
                .SendAsync("PlayerJoined", new
                {
                    PlayerId = player.Id,
                    Nickname = nickname,
                    TotalPlayers = updatedRoom.Players.Count
                });
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task Reconnect(string code, Guid playerId)
    {
        try
        {
            var player = await _roomService
                .ReconnectAsync(code, playerId, Context.ConnectionId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Could not reconnect");
                return;
            }

            // Cancel disconnect timer
            if (_disconnectTimers.TryGetValue(playerId, out var cts))
            {
                cts.Cancel();
                _disconnectTimers.Remove(playerId);
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId, player.RoomId.ToString());

            await Clients.Group(player.RoomId.ToString())
                .SendAsync("PlayerReconnected", new
                {
                    PlayerId = player.Id,
                    Nickname = player.Nickname
                });

            // Send current hand to reconnected player
            var hand = await _gameService.GetPlayerHandAsync(playerId);
            await Clients.Caller.SendAsync("HandDealt", hand);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    // ─── GAME ────────────────────────────────────────────────

    public async Task StartGame(Guid roomId)
    {
        try
        {
            var player = await _roomService
                .GetPlayerByConnectionAsync(Context.ConnectionId);

            if (player == null || !player.IsHost)
            {
                await Clients.Caller.SendAsync("Error", "Only host can start");
                return;
            }

            var round = await _gameService.StartGameAsync(roomId);

            await Clients.Group(roomId.ToString())
                .SendAsync("GameStarted", new { RoomId = roomId });

            // Deal hands privately to each player
            await DealHandsToPlayersAsync(roomId);

            // Start first round
            await Clients.Group(roomId.ToString())
                .SendAsync("RoundStarted", round);

            // Notify first player it's their turn
            await NotifyCurrentPlayerAsync(round);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task SubmitCard(Guid roundId, Guid cardId)
    {
        try
        {
            var player = await _roomService
                .GetPlayerByConnectionAsync(Context.ConnectionId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            // Cancel turn timer
            if (_turnTimers.TryGetValue(roundId, out var cts))
            {
                cts.Cancel();
                _turnTimers.Remove(roundId);
            }

            var cardPlay = await _gameService
                .SubmitCardAsync(roundId, player.Id, cardId);

            // Broadcast card reveal to everyone
            await Clients.Group(player.RoomId.ToString())
                .SendAsync("CardRevealed", cardPlay);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task SubmitVote(Guid cardPlayId, string voteType)
    {
        try
        {
            var player = await _roomService
                .GetPlayerByConnectionAsync(Context.ConnectionId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            var vote = await _gameService
                .SubmitVoteAsync(cardPlayId, player.Id, voteType);

            // Broadcast live vote to everyone
            await Clients.Group(player.RoomId.ToString())
                .SendAsync("VoteReceived", new
                {
                    CardPlayId = cardPlayId,
                    Vote = vote
                });

            // Check if turn is complete
            if (await _gameService.IsTurnCompleteAsync(cardPlayId))
                await AdvanceTurnAsync(cardPlayId, player.RoomId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────

    private async Task AdvanceTurnAsync(Guid cardPlayId, Guid roomId)
    {
        // Get round from cardplay
        var cardPlay = await _gameService.GetCardPlayWithRoundAsync(cardPlayId);
    if (cardPlay == null) return;

        var round = cardPlay.Round;

        await Clients.Group(roomId.ToString())
            .SendAsync("TurnEnded", new
            {
                TurnIndex = round.CurrentTurnIndex
            });

        // Advance turn index
        await _gameService.SkipTurnAsync(round.Id);
        round.CurrentTurnIndex++;

        // Check if round complete
        if (await _gameService.IsRoundCompleteAsync(round.Id))
        {
            await EndRoundAsync(round.Id, roomId);
            return;
        }

        // Reload updated round
        var updatedRound = await GetRoundDtoAsync(round.Id);
        if (updatedRound == null) return;

        await Clients.Group(roomId.ToString())
            .SendAsync("TurnStarted", new
            {
                CurrentPlayerId = updatedRound.CurrentPlayerId,
                TurnIndex = updatedRound.CurrentTurnIndex,
                TotalTurns = updatedRound.TotalTurns
            });

        // Notify current player privately + start timer
        await NotifyCurrentPlayerAsync(updatedRound);
        await StartTurnTimerAsync(round.Id, updatedRound.CurrentPlayerId, roomId);
    }

    private async Task EndRoundAsync(Guid roundId, Guid roomId)
    {
        var results = await _gameService.EndRoundAsync(roundId);

        await Clients.Group(roomId.ToString())
            .SendAsync("RoundEnded", results);

        // Deal new cards to each player
        await DrawNewCardsAsync(roomId);

        // Short delay for results display
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Check game over
        var room = await GetRoomAsync(roomId);
        if (room == null) return;

        if (room.CurrentRound >= room.TotalRounds)
        {
            await Clients.Group(roomId.ToString())
                .SendAsync("GameOver", new
                {
                    FinalScores = results.Scores
                });
            return;
        }

        // Start next round
        var nextRound = await _gameService.StartNextRoundAsync(roomId);

        await Clients.Group(roomId.ToString())
            .SendAsync("RoundStarted", nextRound);

        await NotifyCurrentPlayerAsync(nextRound);
        await StartTurnTimerAsync(
            nextRound.Id, nextRound.CurrentPlayerId, roomId);
    }

    private async Task StartTurnTimerAsync(
        Guid roundId, Guid currentPlayerId, Guid roomId)
    {
        var cts = new CancellationTokenSource();
        _turnTimers[roundId] = cts;

        await Clients.Group(roomId.ToString())
            .SendAsync("TurnTimerStarted", new { Seconds = 15 });

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);

                // Timer expired → skip turn
                await Clients.Group(roomId.ToString())
                    .SendAsync("TurnSkipped", new
                    {
                        PlayerId = currentPlayerId,
                        Reason = "Timer expired"
                    });

                await _gameService.SkipTurnAsync(roundId);
                var updatedRound = await GetRoundDtoAsync(roundId);
                if (updatedRound == null) return;

                if (await _gameService.IsRoundCompleteAsync(roundId))
                {
                    await EndRoundAsync(roundId, roomId);
                    return;
                }

                await Clients.Group(roomId.ToString())
                    .SendAsync("TurnStarted", new
                    {
                        CurrentPlayerId = updatedRound.CurrentPlayerId,
                        TurnIndex = updatedRound.CurrentTurnIndex,
                        TotalTurns = updatedRound.TotalTurns
                    });

                await NotifyCurrentPlayerAsync(updatedRound);
                await StartTurnTimerAsync(
                    roundId, updatedRound.CurrentPlayerId, roomId);
            }
            catch (TaskCanceledException) { }
        }, cts.Token);
    }

    private async Task NotifyCurrentPlayerAsync(RoundDto round)
    {
        var currentPlayer = await _roomService
            .GetPlayerByIdAsync(round.CurrentPlayerId);

        if (currentPlayer == null) return;

        await Clients.Client(currentPlayer.ConnectionId)
            .SendAsync("YourTurn", new
            {
                RoundId = round.Id,
                TurnIndex = round.CurrentTurnIndex
            });
    }

    private async Task PromoteHostAsync(Guid roomId)
    {
        await _roomService.PromoteNewHostAsync(roomId);

        var newHost = await GetNewHostAsync(roomId);
        if (newHost == null) return;

        await Clients.Group(roomId.ToString())
            .SendAsync("HostChanged", new
            {
                NewHostId = newHost.Id,
                Nickname = newHost.Nickname
            });
    }

    private async Task DealHandsToPlayersAsync(Guid roomId)
    {
        var players = await GetActivePlayersAsync(roomId);

        foreach (var player in players)
        {
            var hand = await _gameService.GetPlayerHandAsync(player.Id);

            await Clients.Client(player.ConnectionId)
                .SendAsync("HandDealt", hand);
        }
    }

    private async Task DrawNewCardsAsync(Guid roomId)
    {
        var players = await GetActivePlayersAsync(roomId);

        foreach (var player in players)
        {
            await _gameService.DrawCardAsync(player.Id, roomId);
            var hand = await _gameService.GetPlayerHandAsync(player.Id);

            await Clients.Client(player.ConnectionId)
                .SendAsync("NewCardDealt", hand);
        }
    }

private async Task<CardPlayWithRoundDto?> GetCardPlayWithRoundAsync(Guid cardPlayId)
{
    return await _gameService.GetCardPlayWithRoundAsync(cardPlayId);
}

private async Task<RoundDto?> GetRoundDtoAsync(Guid roundId)
{
    return await _gameService.GetRoundDtoAsync(roundId);
}

private async Task<dynamic?> GetRoomAsync(Guid roomId)
{
    return await _roomService.GetRoomByCodeAsync(roomId.ToString());
}

private async Task<dynamic?> GetNewHostAsync(Guid roomId)
{
    return await _roomService.GetActivePlayersAsync(roomId)
        .ContinueWith(t => t.Result
            .FirstOrDefault(p => p.IsHost) as dynamic);
}

private async Task<List<Player>> GetActivePlayersAsync(Guid roomId)
{
    return await _roomService.GetActivePlayersAsync(roomId);
}


}