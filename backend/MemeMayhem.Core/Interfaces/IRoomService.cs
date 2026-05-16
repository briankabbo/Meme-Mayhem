using MemeMayhem.Core.DTOs;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Core.Interfaces;

public interface IRoomService
{
    Task<Room> CreateRoomAsync(string nickname, string theme, int totalRounds);
    Task<(Room room, Player player)> JoinRoomAsync(string code, string nickname);
    Task<Player> JoinAsSpectatorAsync(string code, string nickname);
    Task<Player?> ReconnectAsync(string code, Guid playerId, string newConnectionId);
    Task PromoteNewHostAsync(Guid roomId);
    Task<Room?> GetRoomByCodeAsync(string code);
    Task<Player?> GetPlayerByConnectionAsync(string connectionId);
    Task<Player?> GetPlayerByIdAsync(Guid playerId);         // ← added
    Task<List<Player>> GetActivePlayersAsync(Guid roomId);   // ← added
    Task UpdateConnectionAsync(Guid playerId, string connectionId, bool isConnected);
}