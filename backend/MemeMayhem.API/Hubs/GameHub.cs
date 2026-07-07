using MemeMayhem.Core.Entities;
using MemeMayhem.Core.DTOs;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Core.Enums;
using MemeMayhem.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace MemeMayhem.API.Hubs;

public class GameHub : Hub
{
    private const int TurnTimeoutSeconds = 15;
    private const int VoteTimeoutSeconds = 10;
    private const int DisconnectGraceSeconds = 30;

    private readonly IRoomService _roomService;
    private readonly IGameService _gameService;
    private readonly IServiceScopeFactory _scopeFactory; //Create fresh DI scopes inside background timer callbacks

    private static readonly Dictionary<Guid, CancellationTokenSource> _turnTimers = new();
    private static readonly Dictionary<Guid, CancellationTokenSource> _disconnectTimers = new();
    private static readonly Dictionary<Guid, CancellationTokenSource> _voteTimers = new();
    private static readonly Dictionary<Guid, DateTime> _voteTimerStartedAt = new();
    private static readonly HashSet<Guid> _advancingCardPlays = new();
    private static readonly object _advancingLock = new();
    public GameHub(IRoomService roomService, IGameService gameService, IServiceScopeFactory scopeFactory)
    {
        _roomService = roomService;
        _gameService = gameService;
        _scopeFactory = scopeFactory;
    }

    // Connection

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

        await ResolveDisconnectGameStateAsync(player, _gameService, _roomService);

        if (player.IsHost)
            await PromoteHostAsync(player.RoomId);

        // 30s grace period before auto-skip
        var cts = new CancellationTokenSource();
        lock (_disconnectTimers)
        {
            _disconnectTimers[player.Id] = cts;
        }

        _ = Task.Run(async () =>
        {
            using (cts)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(DisconnectGraceSeconds), cts.Token);

                    // one is already safe without a fresh scope.
                    await Clients.Group(player.RoomId.ToString())
                        .SendAsync("PlayerTimedOut", new
                        {
                            PlayerId = player.Id,
                            Nickname = player.Nickname
                        });
                }
                catch (TaskCanceledException) { }
                finally
                {
                    lock (_disconnectTimers)
                    {
                        _disconnectTimers.Remove(player.Id);
                    }
                }
            }
        }, cts.Token);

        await base.OnDisconnectedAsync(exception);
    }

    // Room

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
                IsHost = true,
                Theme = room.Theme,
                TotalRounds = room.TotalRounds,
                Players = room.Players.Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Nickname = p.Nickname,
                    IsHost = p.IsHost,
                    IsSpectator = p.IsSpectator,
                    IsConnected = p.IsConnected,
                    TotalScore = p.TotalScore
                }).ToList()
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

            // Game already started - spectator
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
                IsHost = player.IsHost,
                Code = updatedRoom.Code,
                Theme = updatedRoom.Theme,
                TotalRounds = updatedRoom.TotalRounds,
                Players = updatedRoom.Players.Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Nickname = p.Nickname,
                    IsHost = p.IsHost,
                    IsSpectator = p.IsSpectator,
                    IsConnected = p.IsConnected,
                    TotalScore = p.TotalScore
                }).ToList()
            });

            await Clients.Group(updatedRoom.Id.ToString())
                .SendAsync("PlayerJoined", new PlayerDto
                {
                    Id = player.Id,
                    Nickname = player.Nickname,
                    IsHost = player.IsHost,
                    IsSpectator = player.IsSpectator,
                    IsConnected = player.IsConnected,
                    TotalScore = player.TotalScore
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
            lock (_disconnectTimers)
            {
                if (_disconnectTimers.TryGetValue(playerId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _disconnectTimers.Remove(playerId);
                }
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId, player.RoomId.ToString());

            await Clients.Group(player.RoomId.ToString())
                .SendAsync("PlayerReconnected", new
                {
                    PlayerId = player.Id,
                    Nickname = player.Nickname
                });

            // Send complete GameStateSync
            var room = await _roomService.GetRoomByIdAsync(player.RoomId);
            if (room == null) return;

            RoundDto? currentRoundDto = null;
            if (room.Status == RoomStatus.Active)
            {
                var activeRound = await _gameService.GetCurrentRoundForRoomAsync(room.Id);
                if (activeRound != null)
                {
                    currentRoundDto = await _gameService.GetRoundWithCardPlaysAsync(activeRound.Id);
                }
            }

            var hand = await _gameService.GetPlayerHandAsync(playerId);

            Guid? activeCardPlayId = null;
            int? voteTimerSeconds = null;
            if (room.Status == RoomStatus.Active)
            {
                activeCardPlayId = await _gameService.GetActiveCardPlayAsync(room.Id);
                if (activeCardPlayId.HasValue)
                    voteTimerSeconds = GetRemainingVoteSeconds(activeCardPlayId.Value);
            }

            await Clients.Caller.SendAsync("GameStateSync", new
            {
                RoomId = room.Id.ToString(),
                RoomCode = room.Code,
                RoomStatus = room.Status.ToString(),
                PlayerId = player.Id.ToString(),
                IsHost = player.IsHost,
                IsSpectator = player.IsSpectator,
                Theme = room.Theme,
                TotalRounds = room.TotalRounds,
                CurrentRoundNumber = currentRoundDto?.RoundNumber ?? 0,
                Players = room.Players.Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Nickname = p.Nickname,
                    IsHost = p.IsHost,
                    IsSpectator = p.IsSpectator,
                    IsConnected = p.IsConnected,
                    TotalScore = p.TotalScore
                }).ToList(),
                CurrentRound = currentRoundDto,
                Hand = hand,
                IsMyTurn = currentRoundDto?.CurrentPlayerId == player.Id,
                ActiveCardPlayId = activeCardPlayId,
                VoteTimerSeconds = voteTimerSeconds,
            });
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    // Game

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

            // 1. Start game internally — deals cards to DB
            var round = await _gameService.StartGameAsync(roomId);

            // 2. Deal hands privately first
            try
            {
                await DealHandsToPlayersAsync(roomId, _gameService, _roomService);
            }
            catch (Exception dealEx)
            {
                await Clients.Caller.SendAsync("Error", $"Deal failed: {dealEx.Message}");
                return;
            }

            // 3. Send RoundStarted — currentRound is set BEFORE UI transitions
            await Clients.Group(roomId.ToString())
                .SendAsync("RoundStarted", round);

            // 4. Send GameStarted LAST — UI transitions with data already ready
            await Clients.Group(roomId.ToString())
                .SendAsync("GameStarted", new { RoomId = roomId });

            // 5. Notify first player it's their turn
            await NotifyCurrentPlayerAsync(round, _roomService);

            // FIX: this call was missing entirely — turn 1 never got a server-side
            // timer, so TurnTimerStarted never fired and clients defaulted to 0s,
            // and a genuinely idle first player would never get auto-skipped.
            // Every other turn-start path (AdvanceTurnAsync, EndRoundAsync,
            // HandleTurnSkippedAsync) already calls this — StartGame was the
            // only one that didn't.
            await StartTurnTimerAsync(round.Id, round.CurrentPlayerId, roomId);
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
            lock (_turnTimers)
            {
                if (_turnTimers.TryGetValue(roundId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _turnTimers.Remove(roundId);
                }
            }

            var cardPlay = await _gameService
                .SubmitCardAsync(roundId, player.Id, cardId);

            // Broadcast card reveal to everyone
            await Clients.Group(player.RoomId.ToString())
                .SendAsync("CardRevealed", cardPlay);

            await StartVoteTimerAsync(cardPlay.Id, player.RoomId);
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
            {
                CancelVoteTimer(cardPlayId);
                await AdvanceTurnAsync(cardPlayId, player.RoomId, _gameService, _roomService);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    private async Task ResolveDisconnectGameStateAsync(
        Player player, IGameService gameService, IRoomService roomService)
    {
        var currentRound = await gameService.GetCurrentRoundForRoomAsync(player.RoomId);
        if (currentRound == null) return;

        // TurnSkipped first when the disconnecting player holds the active card-picking turn.
        if (currentRound.CurrentPlayerId == player.Id &&
            currentRound.Status == RoundStatus.CardPicking.ToString())
        {
            lock (_turnTimers)
            {
                if (_turnTimers.TryGetValue(currentRound.Id, out var timerCts))
                {
                    timerCts.Cancel();
                    timerCts.Dispose();
                    _turnTimers.Remove(currentRound.Id);
                }
            }

            await HandleTurnSkippedAsync(currentRound.Id, player.Id, player.RoomId, gameService, roomService);
        }

        // Re-evaluate vote completion — disconnected players are excluded from required vote count.
        var activeCardPlayId = await gameService.GetActiveCardPlayAsync(player.RoomId);
        if (!activeCardPlayId.HasValue) return;

        if (await gameService.IsTurnCompleteAsync(activeCardPlayId.Value))
        {
            CancelVoteTimer(activeCardPlayId.Value);
            await AdvanceTurnAsync(activeCardPlayId.Value, player.RoomId, gameService, roomService);
        }
    }

    private async Task AdvanceTurnAsync(
        Guid cardPlayId, Guid roomId, IGameService gameService, IRoomService roomService)
    {
        lock (_advancingLock)
        {
            if (!_advancingCardPlays.Add(cardPlayId))
                return;
        }

        try
        {
            // Get round from cardplay
            var cardPlay = await gameService.GetCardPlayWithRoundAsync(cardPlayId);
            if (cardPlay == null) return;

            var round = cardPlay.Round;

            await Clients.Group(roomId.ToString())
                .SendAsync("TurnEnded", new
                {
                    TurnIndex = round.CurrentTurnIndex
                });

            // Advance turn index
            await gameService.SkipTurnAsync(round.Id);

            // Check if round complete
            if (await gameService.IsRoundCompleteAsync(round.Id))
            {
                await EndRoundAsync(round.Id, roomId, gameService, roomService);
                return;
            }

            // Reload updated round
            var updatedRound = await gameService.GetRoundDtoAsync(round.Id);
            if (updatedRound == null) return;

            await Clients.Group(roomId.ToString())
                .SendAsync("TurnStarted", new
                {
                    CurrentPlayerId = updatedRound.CurrentPlayerId,
                    TurnIndex = updatedRound.CurrentTurnIndex,
                    TotalTurns = updatedRound.TotalTurns
                });

            // Notify current player privately + start timer
            await NotifyCurrentPlayerAsync(updatedRound, roomService);
            await StartTurnTimerAsync(round.Id, updatedRound.CurrentPlayerId, roomId);
        }
        finally
        {
            lock (_advancingLock)
            {
                _advancingCardPlays.Remove(cardPlayId);
            }
        }
    }

    private async Task EndRoundAsync(
        Guid roundId, Guid roomId, IGameService gameService, IRoomService roomService)
    {
        var results = await gameService.EndRoundAsync(roundId);

        await Clients.Group(roomId.ToString())
            .SendAsync("RoundEnded", results);

        // Short delay for results display
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Check game over BEFORE drawing new cards
        if (results.IsGameOver)
        {
            await Clients.Group(roomId.ToString())
                .SendAsync("GameOver", new
                {
                    FinalScores = results.Scores
                });
            return;
        }

        // Only draw new cards if game continues
        await DrawNewCardsAsync(roomId, gameService, roomService);

        // Start next round
        var nextRound = await gameService.StartNextRoundAsync(roomId);

        // Deal new hands before showing next round
        await DealHandsToPlayersAsync(roomId, gameService, roomService);

        await Clients.Group(roomId.ToString())
            .SendAsync("RoundStarted", nextRound);

        await NotifyCurrentPlayerAsync(nextRound, roomService);
        await StartTurnTimerAsync(
            nextRound.Id, nextRound.CurrentPlayerId, roomId);
    }

    private async Task StartTurnTimerAsync(
        Guid roundId, Guid currentPlayerId, Guid roomId)
    {
        var cts = new CancellationTokenSource();
        lock (_turnTimers)
        {
            _turnTimers[roundId] = cts;
        }

        await Clients.Group(roomId.ToString())
            .SendAsync("TurnTimerStarted", new { Seconds = TurnTimeoutSeconds });

        _ = Task.Run(async () =>
        {
            using (cts)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(TurnTimeoutSeconds), cts.Token);
                    using var scope = _scopeFactory.CreateScope();
                    var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

                    await HandleTurnSkippedAsync(roundId, currentPlayerId, roomId, gameService, roomService);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    await Clients.Group(roomId.ToString())
                        .SendAsync("Error", $"Turn timer error: {ex.Message}");
                }
                finally
                {
                    lock (_turnTimers)
                    {
                        _turnTimers.Remove(roundId);
                    }
                }
            }
        }, cts.Token);
    }

    private async Task HandleTurnSkippedAsync(
        Guid roundId, Guid currentPlayerId, Guid roomId, IGameService gameService, IRoomService roomService)
    {
        await Clients.Group(roomId.ToString())
            .SendAsync("TurnSkipped", new
            {
                PlayerId = currentPlayerId,
                Reason = "Timer expired or disconnected"
            });

        await gameService.SkipTurnAsync(roundId);
        var updatedRound = await gameService.GetRoundDtoAsync(roundId);
        if (updatedRound == null) return;

        if (await gameService.IsRoundCompleteAsync(roundId))
        {
            await EndRoundAsync(roundId, roomId, gameService, roomService);
            return;
        }

        await Clients.Group(roomId.ToString())
            .SendAsync("TurnStarted", new
            {
                CurrentPlayerId = updatedRound.CurrentPlayerId,
                TurnIndex = updatedRound.CurrentTurnIndex,
                TotalTurns = updatedRound.TotalTurns
            });

        await NotifyCurrentPlayerAsync(updatedRound, roomService);
        await StartTurnTimerAsync(roundId, updatedRound.CurrentPlayerId, roomId);
    }

    private async Task StartVoteTimerAsync(Guid cardPlayId, Guid roomId)
    {
        var cts = new CancellationTokenSource();
        lock (_voteTimers)
        {
            _voteTimers[cardPlayId] = cts;
        }

        lock (_voteTimerStartedAt)
        {
            _voteTimerStartedAt[cardPlayId] = DateTime.UtcNow;
        }

        await Clients.Group(roomId.ToString())
            .SendAsync("VoteTimerStarted", new { Seconds = VoteTimeoutSeconds });

        _ = Task.Run(async () =>
        {
            using (cts)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(VoteTimeoutSeconds), cts.Token);

                    // resolve fresh scoped services before hitting the database.
                    using var scope = _scopeFactory.CreateScope();
                    var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

                    // Timer expired → auto-vote Meh for missing voters
                    var missingVoters = await gameService.GetMissingVotersAsync(cardPlayId, roomId);

                    foreach (var voterId in missingVoters)
                    {
                        try
                        {
                            var vote = await gameService.SubmitVoteAsync(cardPlayId, voterId, "Meh");
                            await Clients.Group(roomId.ToString())
                                .SendAsync("VoteReceived", new
                                {
                                    CardPlayId = cardPlayId,
                                    Vote = vote
                                });
                        }
                        catch (Exception)
                        {
                            // Ignore errors for individual auto-votes
                        }
                    }

                    // Advance turn if all votes are now in
                    if (await gameService.IsTurnCompleteAsync(cardPlayId))
                    {
                        await AdvanceTurnAsync(cardPlayId, roomId, gameService, roomService);
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    // NEW: same reasoning as the turn timer — don't let this die silently.
                    await Clients.Group(roomId.ToString())
                        .SendAsync("Error", $"Vote timer error: {ex.Message}");
                }
                finally
                {
                    lock (_voteTimers)
                    {
                        _voteTimers.Remove(cardPlayId);
                    }
                    lock (_voteTimerStartedAt)
                    {
                        _voteTimerStartedAt.Remove(cardPlayId);
                    }
                }
            }
        }, cts.Token);
    }

    private int? GetRemainingVoteSeconds(Guid cardPlayId)
    {
        lock (_voteTimerStartedAt)
        {
            if (!_voteTimerStartedAt.TryGetValue(cardPlayId, out var startedAt))
                return null;

            var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;
            var remaining = VoteTimeoutSeconds - (int)Math.Floor(elapsed);
            return remaining > 0 ? remaining : 0;
        }
    }

    private void CancelVoteTimer(Guid cardPlayId)
    {
        lock (_voteTimers)
        {
            if (_voteTimers.TryGetValue(cardPlayId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _voteTimers.Remove(cardPlayId);
            }
        }
        lock (_voteTimerStartedAt)
        {
            _voteTimerStartedAt.Remove(cardPlayId);
        }
    }

    private async Task NotifyCurrentPlayerAsync(RoundDto round, IRoomService roomService)
    {
        var currentPlayer = await roomService
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
        // Only ever called from OnDisconnectedAsync's live scope, so the
        await _roomService.PromoteNewHostAsync(roomId);

        var newHost = await GetNewHostAsync(roomId, _roomService);
        if (newHost == null) return;

        await Clients.Group(roomId.ToString())
            .SendAsync("HostChanged", new
            {
                NewHostId = newHost.Id,
                Nickname = newHost.Nickname
            });
    }

    private async Task DealHandsToPlayersAsync(
        Guid roomId, IGameService gameService, IRoomService roomService)
    {
        var players = await GetActivePlayersAsync(roomId, roomService);

        foreach (var player in players)
        {
            var hand = await gameService.GetPlayerHandAsync(player.Id);

            await Clients.Client(player.ConnectionId)
                .SendAsync("HandDealt", hand);
        }
    }

    private async Task DrawNewCardsAsync(
        Guid roomId, IGameService gameService, IRoomService roomService)
    {
        var players = await GetActivePlayersAsync(roomId, roomService);

        foreach (var player in players)
        {
            await gameService.DrawCardAsync(player.Id, roomId);
            var hand = await gameService.GetPlayerHandAsync(player.Id);

            await Clients.Client(player.ConnectionId)
                .SendAsync("NewCardDealt", hand);
        }
    }

    private async Task<Room?> GetRoomAsync(Guid roomId)
    {
        return await _roomService.GetRoomByIdAsync(roomId);
    }

    private async Task<Player?> GetNewHostAsync(Guid roomId, IRoomService roomService)
    {
        var players = await roomService.GetActivePlayersAsync(roomId);
        return players.FirstOrDefault(p => p.IsHost);
    }

    private async Task<List<Player>> GetActivePlayersAsync(Guid roomId, IRoomService roomService)
    {
        return await roomService.GetActivePlayersAsync(roomId);
    }
}