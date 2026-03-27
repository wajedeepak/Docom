using Docom.Domain.Entities;
using Docom.Domain.Enums;

namespace Docom.Domain.Interfaces.Repositories;

public interface ITokenRepository
{
    /// <summary>
    /// Atomically increments LastIssuedTokenNumber on Session, then inserts the token.
    /// Uses a serializable transaction to prevent duplicate token numbers.
    /// </summary>
    Task<Token> CreateAsync(Token token);

    Task<IEnumerable<Token>> GetQueueBySessionAsync(int sessionId);
    Task<Token?> GetCurrentServingAsync(int sessionId);
    Task<Token?> GetNextWaitingAsync(int sessionId);
    Task<Token?> GetByIdAsync(long tokenId);
    Task UpdateAsync(Token token);
    Task<int> GetWaitingCountAsync(int sessionId);

    /// <summary>
    /// Moves a skipped token to the end of the queue by updating QueueOrder.
    /// </summary>
    Task RequeueSkippedAsync(long tokenId, int sessionId);

    Task<Token?> GetByPublicTokenIdAsync(string publicTokenId);
}
