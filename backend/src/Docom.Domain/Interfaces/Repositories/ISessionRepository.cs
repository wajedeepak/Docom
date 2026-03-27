using Docom.Domain.Entities;
using Docom.Domain.Enums;

namespace Docom.Domain.Interfaces.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetActiveSessionByDoctorIdAsync(int doctorId);
    Task<Session?> GetByIdAsync(int id);
    Task<IEnumerable<Session>> GetByDoctorIdAsync(int doctorId, int limit = 10);
    Task<Session> CreateAsync(Session session);
    Task UpdateAsync(Session session);
    Task UpdateStatusAsync(int sessionId, SessionStatus status);
}
