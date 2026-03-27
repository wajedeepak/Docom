using Docom.Domain.Entities;
using Docom.Domain.Interfaces.Repositories;
using Docom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Docom.Infrastructure.Repositories;

public class OtpRepository : IOtpRepository
{
    private readonly AppDbContext _db;

    public OtpRepository(AppDbContext db) => _db = db;

    public async Task<OtpRequest> CreateAsync(OtpRequest request)
    {
        _db.OtpRequests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public Task<OtpRequest?> GetLatestValidAsync(string email)
        => _db.OtpRequests
              .Where(o => o.Email == email && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
              .OrderByDescending(o => o.CreatedAt)
              .FirstOrDefaultAsync();

    public async Task MarkUsedAsync(int id)
        => await _db.OtpRequests
                    .Where(o => o.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsUsed, true));

    public async Task InvalidatePreviousAsync(string email)
        => await _db.OtpRequests
                    .Where(o => o.Email == email && !o.IsUsed)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsUsed, true));
}
