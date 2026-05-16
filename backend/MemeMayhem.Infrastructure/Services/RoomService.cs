using MemeMayhem.Core.Entities;
using MemeMayhem.Core.Enums;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MemeMayhem.Infrastructure.Services;

public class RoomService : IRoomService
{
    private readonly MemeMayhemDbContext _db;

    public RoomService(MemeMayhemDbContext db)
    {
        _db = db;
    }

    // Create Room

    public async Task<Room> CreateRoomAsync(
        string nickname, string theme, int totalRounds)
    {
        // Validate rounds
        if (totalRounds < 1 || totalRounds > 20)
            throw new InvalidOperationException(
                "Rounds must be between 1 and 20");

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Code = await GenerateUniqueCodeAsync(),
            Theme = theme,
            Status = RoomStatus.Lobby,
            TotalRounds = totalRounds,
            CurrentRound = 0,
            CreatedAt = DateTime.UtcNow
        };

        var host = new Player
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Nickname = nickname,
            IsHost = true,
            IsSpectator = false,
            IsConnected = true,
            TotalScore = 0,
            JoinedAt = DateTime.UtcNow
        };

        room.HostPlayerId = host.Id;

        await _db.Rooms.AddAsync(room);
        await _db.Players.AddAsync(host);
        await _db.SaveChangesAsync();

        // Reload with players
        return await _db.Rooms
            .Include(r => r.Players)
            .FirstAsync(r => r.Id == room.Id);
    }

    // Join Room

    public async Task<(Room room, Player player)> JoinRoomAsync(
        string code, string nickname)
    {
        var room = await _db.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Code == code)
            ?? throw new InvalidOperationException("Room not found");

        if (room.Status != RoomStatus.Lobby)
            throw new InvalidOperationException("Game already started");

        // Count active players (not spectators)
        var activePlayers = room.Players
            .Count(p => !p.IsSpectator);

        if (activePlayers >= 10)
            throw new InvalidOperationException("Room is full (max 10 players)");

        // Warn if only 2 players
        var player = new Player
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Nickname = nickname,
            IsHost = false,
            IsSpectator = false,
            IsConnected = true,
            TotalScore = 0,
            JoinedAt = DateTime.UtcNow
        };

        await _db.Players.AddAsync(player);
        await _db.SaveChangesAsync();

        // Reload room with updated players
        var updatedRoom = await _db.Rooms
            .Include(r => r.Players)
            .FirstAsync(r => r.Id == room.Id);

        return (updatedRoom, player);
    }

    // Join as Spectator

    public async Task<Player> JoinAsSpectatorAsync(string code, string nickname)
    {
        var room = await _db.Rooms
            .FirstOrDefaultAsync(r => r.Code == code)
            ?? throw new InvalidOperationException("Room not found");

        var spectator = new Player
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Nickname = nickname,
            IsHost = false,
            IsSpectator = true,
            IsConnected = true,
            TotalScore = 0,
            JoinedAt = DateTime.UtcNow
        };

        await _db.Players.AddAsync(spectator);
        await _db.SaveChangesAsync();

        return spectator;
    }

    // Reconnect

    public async Task<Player?> ReconnectAsync(
        string code, Guid playerId, string newConnectionId)
    {
        var room = await _db.Rooms
            .FirstOrDefaultAsync(r => r.Code == code);

        if (room == null) return null;

        var player = await _db.Players
            .FirstOrDefaultAsync(p =>
                p.Id == playerId &&
                p.RoomId == room.Id);

        if (player == null) return null;

        player.ConnectionId = newConnectionId;
        player.IsConnected = true;
        player.DisconnectedAt = null;

        await _db.SaveChangesAsync();

        return player;
    }

    // Promote Host

    public async Task PromoteNewHostAsync(Guid roomId)
    {
        var room = await _db.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) return;

        // Find next eligible player
        // Not spectator, connected, not current host
        var nextHost = room.Players
            .Where(p =>
                !p.IsSpectator &&
                p.IsConnected &&
                !p.IsHost)
            .OrderBy(p => p.JoinedAt)   // earliest joiner becomes host
            .FirstOrDefault();

        if (nextHost == null) return;

        // Demote old host
        var oldHost = room.Players.FirstOrDefault(p => p.IsHost);
        if (oldHost != null)
            oldHost.IsHost = false;

        // Promote new host
        nextHost.IsHost = true;
        room.HostPlayerId = nextHost.Id;

        await _db.SaveChangesAsync();
    }

    // Getters

    public async Task<Room?> GetRoomByCodeAsync(string code)
    {
        return await _db.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Code == code);
    }

    public async Task<Player?> GetPlayerByConnectionAsync(string connectionId)
    {
        return await _db.Players
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId);
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        return await _db.Players
            .FirstOrDefaultAsync(p => p.Id == playerId);
    }

    public async Task<List<Player>> GetActivePlayersAsync(Guid roomId)
    {
        return await _db.Players
            .Where(p =>
                p.RoomId == roomId &&
                !p.IsSpectator &&
                p.IsConnected)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync();
    }

    public async Task UpdateConnectionAsync(
        Guid playerId, string connectionId, bool isConnected)
    {
        var player = await _db.Players.FindAsync(playerId);
        if (player == null) return;

        player.ConnectionId = connectionId;
        player.IsConnected = isConnected;

        if (!isConnected)
            player.DisconnectedAt = DateTime.UtcNow;
        else
            player.DisconnectedAt = null;

        await _db.SaveChangesAsync();
    }

    // Private Helpers

    private async Task<string> GenerateUniqueCodeAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string code;

        // Keep generating until unique
        do
        {
            code = new string(Enumerable
                .Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
        while (await _db.Rooms.AnyAsync(r => r.Code == code));

        return code;
    }
}