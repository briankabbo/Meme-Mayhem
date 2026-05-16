using MemeMayhem.Core.Entities;
using MemeMayhem.Core.Enums;

namespace MemeMayhem.Core.Interfaces;

public interface IGiphyService
{
    Task SyncReactionGifsAsync();
    Task<ReactionGif> GetRandomGifAsync(VoteType voteType);
    Task<List<ReactionGif>> GetGifsByVoteTypeAsync(VoteType voteType);
}