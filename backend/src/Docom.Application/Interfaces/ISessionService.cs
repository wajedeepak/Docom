using Docom.Application.DTOs.Session;

namespace Docom.Application.Interfaces;

public interface ISessionService
{
    Task<SessionDto> CreateSessionAsync(int doctorId, CreateSessionDto dto);
    Task<SessionDto> StartSessionAsync(int sessionId, int doctorId);
    Task<SessionDto> PauseSessionAsync(int sessionId, int doctorId);
    Task<SessionDto> ResumeSessionAsync(int sessionId, int doctorId);
    Task<SessionDto> EndSessionAsync(int sessionId, int doctorId);
    Task<QueueStateDto> GetQueueStateAsync(int sessionId, int doctorId);
    Task<PublicQueueStateDto> GetPublicQueueStateAsync(string doctorSlug);
    Task<SessionDto?> GetActiveSessionAsync(int doctorId);
}
