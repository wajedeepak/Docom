using Docom.Domain.Entities;
using Docom.Domain.Enums;
using Docom.Domain.Interfaces.Repositories;
using Docom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Docom.Infrastructure.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly AppDbContext _db;

    public TokenRepository(AppDbContext db) => _db = db;

    /// <summary>
    /// Atomically increments LastIssuedTokenNumber on Session row (using UPDLOCK),
    /// assigns that number to the new token, and inserts it.
    /// Wrapped in CreateExecutionStrategy to support SqlServerRetryingExecutionStrategy.
    /// </summary>
    public async Task<Token> CreateAsync(Token token)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted);

            // ToListAsync() terminates the SQL without EF appending TOP 1,
            // which would break the non-composable OUTPUT clause.
            var result = await _db.Database
                .SqlQuery<int>($"""
                    UPDATE Sessions WITH (ROWLOCK, UPDLOCK)
                    SET LastIssuedTokenNumber = LastIssuedTokenNumber + 1
                    OUTPUT INSERTED.LastIssuedTokenNumber
                    WHERE Id = {token.SessionId}
                    """)
                .ToListAsync();

            var tokenNumber = result.First();

            token.TokenNumber = tokenNumber;
            token.QueueOrder  = tokenNumber;

            _db.Tokens.Add(token);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        });

        return token;
    }

    public async Task<IEnumerable<Token>> GetQueueBySessionAsync(int sessionId)
        => await _db.Tokens
                    .AsNoTracking()
                    .Where(t => t.SessionId == sessionId &&
                                (t.Status == TokenStatus.Waiting || t.Status == TokenStatus.Serving))
                    .OrderBy(t => t.QueueOrder)
                    .ToListAsync();

    public Task<Token?> GetCurrentServingAsync(int sessionId)
        => _db.Tokens
              .FirstOrDefaultAsync(t => t.SessionId == sessionId && t.Status == TokenStatus.Serving);

    public Task<Token?> GetNextWaitingAsync(int sessionId)
        => _db.Tokens
              .Where(t => t.SessionId == sessionId && t.Status == TokenStatus.Waiting)
              .OrderBy(t => t.QueueOrder)
              .FirstOrDefaultAsync();

    public Task<Token?> GetByIdAsync(long tokenId)
        => _db.Tokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tokenId);

    public async Task UpdateAsync(Token token)
    {
        _db.Tokens.Update(token);
        await _db.SaveChangesAsync();
    }

    public Task<int> GetWaitingCountAsync(int sessionId)
        => _db.Tokens.CountAsync(t => t.SessionId == sessionId && t.Status == TokenStatus.Waiting);

    public async Task RequeueSkippedAsync(long tokenId, int sessionId)
    {
        var maxOrder = await _db.Tokens
            .Where(t => t.SessionId == sessionId &&
                        (t.Status == TokenStatus.Waiting || t.Status == TokenStatus.Skipped))
            .MaxAsync(t => (int?)t.QueueOrder) ?? 0;

        await _db.Tokens
                 .Where(t => t.Id == tokenId)
                 .ExecuteUpdateAsync(s => s
                     .SetProperty(t => t.QueueOrder, maxOrder + 1)
                     .SetProperty(t => t.Status, TokenStatus.Waiting));
    }

    public Task<Token?> GetByPublicTokenIdAsync(string publicTokenId)
        => _db.Tokens.AsNoTracking().FirstOrDefaultAsync(t => t.PublicTokenId == publicTokenId);
}
