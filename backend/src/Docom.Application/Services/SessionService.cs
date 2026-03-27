using Docom.Application.DTOs.Session;
using Docom.Application.DTOs.Token;
using Docom.Application.Interfaces;
using Docom.Domain.Entities;
using Docom.Domain.Enums;
using Docom.Domain.Interfaces.Repositories;
using Docom.Domain.Interfaces.Services;

namespace Docom.Application.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ITokenRepository _tokenRepo;
    private readonly IQueueNotifier _notifier;

    public SessionService(
        ISessionRepository sessionRepo,
        IDoctorRepository doctorRepo,
        ITokenRepository tokenRepo,
        IQueueNotifier notifier)
    {
        _sessionRepo = sessionRepo;
        _doctorRepo = doctorRepo;
        _tokenRepo = tokenRepo;
        _notifier = notifier;
    }

    public async Task<SessionDto> CreateSessionAsync(int doctorId, CreateSessionDto dto)
    {
        var active = await _sessionRepo.GetActiveSessionByDoctorIdAsync(doctorId);
        if (active != null)
            throw new InvalidOperationException("An active session already exists. End it before creating a new one.");

        var session = await _sessionRepo.CreateAsync(new Session
        {
            DoctorId = doctorId,
            Label = dto.Label,
            Status = SessionStatus.Created
        });

        return MapToDto(session);
    }

    public async Task<SessionDto> StartSessionAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedSessionAsync(sessionId, doctorId);

        if (session.Status != SessionStatus.Created)
            throw new InvalidOperationException("Only a Created session can be started.");

        session.Status = SessionStatus.Active;
        session.StartedAt = DateTime.UtcNow;
        await _sessionRepo.UpdateAsync(session);

        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        await _notifier.NotifySessionChangedAsync(doctor!.Slug, "Active");

        return MapToDto(session);
    }

    public async Task<SessionDto> PauseSessionAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedSessionAsync(sessionId, doctorId);

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Only an Active session can be paused.");

        session.Status = SessionStatus.Paused;
        await _sessionRepo.UpdateAsync(session);

        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        await _notifier.NotifySessionChangedAsync(doctor!.Slug, "Paused");

        return MapToDto(session);
    }

    public async Task<SessionDto> ResumeSessionAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedSessionAsync(sessionId, doctorId);

        if (session.Status != SessionStatus.Paused)
            throw new InvalidOperationException("Only a Paused session can be resumed.");

        session.Status = SessionStatus.Active;
        await _sessionRepo.UpdateAsync(session);

        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        await _notifier.NotifySessionChangedAsync(doctor!.Slug, "Active");

        return MapToDto(session);
    }

    public async Task<SessionDto> EndSessionAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedSessionAsync(sessionId, doctorId);

        if (session.Status == SessionStatus.Ended)
            throw new InvalidOperationException("Session is already ended.");

        session.Status = SessionStatus.Ended;
        session.EndedAt = DateTime.UtcNow;
        await _sessionRepo.UpdateAsync(session);

        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        await _notifier.NotifySessionChangedAsync(doctor!.Slug, "Ended");

        return MapToDto(session);
    }

    public async Task<QueueStateDto> GetQueueStateAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedSessionAsync(sessionId, doctorId);
        var queue = await _tokenRepo.GetQueueBySessionAsync(sessionId);
        var waitingCount = await _tokenRepo.GetWaitingCountAsync(sessionId);

        return new QueueStateDto(
            SessionId: session.Id,
            SessionLabel: session.Label,
            SessionStatus: session.Status.ToString(),
            CurrentTokenNumber: session.CurrentTokenNumber,
            LastIssuedTokenNumber: session.LastIssuedTokenNumber,
            WaitingCount: waitingCount,
            EstimatedWaitMinutes: waitingCount * 5,
            Queue: queue.Select(MapTokenToDto)
        );
    }

    public async Task<PublicQueueStateDto> GetPublicQueueStateAsync(string doctorSlug)
    {
        var doctor = await _doctorRepo.GetBySlugAsync(doctorSlug)
            ?? throw new KeyNotFoundException("Doctor not found.");

        if (!doctor.IsActive)
            throw new KeyNotFoundException("Doctor not found.");

        var session = await _sessionRepo.GetActiveSessionByDoctorIdAsync(doctor.Id);

        if (session is null || session.Status == SessionStatus.Ended)
        {
            return new PublicQueueStateDto(
                SessionId: null,
                DoctorName: doctor.Name,
                Specialization: doctor.Specialization,
                SessionActive: false,
                CurrentTokenNumber: 0,
                WaitingCount: 0,
                EstimatedWaitMinutes: 0,
                SessionStatus: "NoSession"
            );
        }

        var waitingCount = await _tokenRepo.GetWaitingCountAsync(session.Id);

        return new PublicQueueStateDto(
            SessionId: session.Id,
            DoctorName: doctor.Name,
            Specialization: doctor.Specialization,
            SessionActive: session.Status == SessionStatus.Active,
            CurrentTokenNumber: session.CurrentTokenNumber,
            WaitingCount: waitingCount,
            EstimatedWaitMinutes: waitingCount * 5,
            SessionStatus: session.Status.ToString()
        );
    }

    public async Task<SessionDto?> GetActiveSessionAsync(int doctorId)
    {
        var session = await _sessionRepo.GetActiveSessionByDoctorIdAsync(doctorId);
        return session is null ? null : MapToDto(session);
    }

    private async Task<Session> GetOwnedSessionAsync(int sessionId, int doctorId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.DoctorId != doctorId)
            throw new UnauthorizedAccessException("You don't own this session.");

        return session;
    }

    private static SessionDto MapToDto(Session s) =>
        new(s.Id, s.Label, s.Status.ToString(), s.CurrentTokenNumber,
            s.LastIssuedTokenNumber, s.CreatedAt, s.StartedAt, s.EndedAt);

    private static TokenDto MapTokenToDto(Token t) =>
        new(t.Id, t.TokenNumber, t.PatientName, t.PhoneNumber, t.Status.ToString(), t.QueueOrder, t.CreatedAt);
}
