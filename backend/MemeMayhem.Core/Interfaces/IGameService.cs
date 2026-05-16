using MemeMayhem.Core.DTOs;
using MemeMayhem.Core.Entities;

namespace MemeMayhem.Core.Interfaces;

public interface IGameService
{
    Task<RoundDto> StartGameAsync(Guid roomId);
    Task<RoundDto> StartNextRoundAsync(Guid roomId);
    Task<CardPlayDto> SubmitCardAsync(Guid roundId, Guid playerId, Guid cardId);
    Task<VoteDto> SubmitVoteAsync(Guid cardPlayId, Guid voterId, string voteType);
    Task<bool> IsTurnCompleteAsync(Guid cardPlayId);
    Task<bool> IsRoundCompleteAsync(Guid roundId);
    Task<RoundResultDto> EndRoundAsync(Guid roundId);
    Task DealCardsAsync(Guid roomId);
    Task DrawCardAsync(Guid playerId, Guid roomId);
    Task SkipTurnAsync(Guid roundId);
    Task<List<MemeCardDto>> GetPlayerHandAsync(Guid playerId);
    Task<CardPlayWithRoundDto?> GetCardPlayWithRoundAsync(Guid cardPlayId);
    Task<RoundDto?> GetRoundDtoAsync(Guid roundId);
}