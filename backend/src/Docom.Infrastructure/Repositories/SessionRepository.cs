using Docom.Domain.Entities;
using Docom.Domain.Enums;
using Docom.Domain.Interfaces.Repositories;
using Docom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Docom.Infrastructure.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _db;

    public SessionRepository(AppDbContext db) => _db = db;

    public Task<Session?> GetActiveSessionByDoctorIdAsync(int doctorId)
        => _db.Sessions
              .AsNoTracking()
              .Where(s => s.DoctorId == doctorId &&
                          s.Status != SessionStatus.Ended)
              .OrderByDescending(s => s.CreatedAt)
              .FirstOrDefaultAsync();

    public Task<Session?> GetByIdAsync(int id)
        => _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Session>> GetByDoctorIdAsync(int doctorId, int limit = 10)
        => await _db.Sessions
                    .AsNoTracking()
                    .Where(s => s.DoctorId == doctorId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

    public async Task<Session> CreateAsync(Session session)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task UpdateAsync(Session session)
    {
        _db.Sessions.Update(session);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int sessionId, SessionStatus status)
        => await _db.Sessions
                    .Where(s => s.Id == sessionId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status));
}
