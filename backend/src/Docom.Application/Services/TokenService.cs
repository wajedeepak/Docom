using System.Linq;
using Docom.Application.DTOs.Token;
using Docom.Application.Interfaces;
using Docom.Domain.Entities;
using Docom.Domain.Enums;
using Docom.Domain.Interfaces.Repositories;
using Docom.Domain.Interfaces.Services;

namespace Docom.Application.Services;

public class TokenService : ITokenService
{
    private readonly ITokenRepository _tokenRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IQueueNotifier _notifier;

    public TokenService(
        ITokenRepository tokenRepo,
        ISessionRepository sessionRepo,
        IDoctorRepository doctorRepo,
        IQueueNotifier notifier)
    {
        _tokenRepo = tokenRepo;
        _sessionRepo = sessionRepo;
        _doctorRepo = doctorRepo;
        _notifier = notifier;
    }

    private static readonly char[] _tokenIdAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

    private static string GeneratePublicTokenId()
    {
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => _tokenIdAlphabet[b % _tokenIdAlphabet.Length]).ToArray());
    }

    public async Task<TakeTokenResponseDto> TakeTokenAsync(string doctorSlug, TakeTokenDto dto)
    {
        var doctor = await _doctorRepo.GetBySlugAsync(doctorSlug)
            ?? throw new KeyNotFoundException("Doctor not found.");

        if (!doctor.IsActive)
            throw new InvalidOperationException("Doctor is not available.");

        var session = await _sessionRepo.GetActiveSessionByDoctorIdAsync(doctor.Id)
            ?? throw new InvalidOperationException("No active session. The doctor has not started a session yet.");

        if (session.Status == SessionStatus.Paused)
            throw new InvalidOperationException("Session is currently paused. Please try again shortly.");

        if (session.Status == SessionStatus.Ended)
            throw new InvalidOperationException("Session has ended.");

        var waitingCount = await _tokenRepo.GetWaitingCountAsync(session.Id);

        // QueueOrder = max existing + 1; handled atomically in repo
        var token = await _tokenRepo.CreateAsync(new Token
        {
            SessionId = session.Id,
            PatientName = dto.PatientName?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Status = TokenStatus.Waiting,
            QueueOrder = waitingCount + 1,
            PublicTokenId = GeneratePublicTokenId()
        });

        var position = waitingCount + 1;

        await _notifier.NotifyTokenCreatedAsync(doctorSlug, token.TokenNumber, position);

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var trackingUrl = $"https://docom.in/t/{token.PublicTokenId}";
        }

        return new TakeTokenResponseDto(
            TokenId: token.Id,
            TokenNumber: token.TokenNumber,
            CurrentTokenNumber: session.CurrentTokenNumber,
            QueuePosition: position,
            EstimatedWaitMinutes: position * 5,
            PublicTokenId: token.PublicTokenId
        );
    }

    public async Task<TokenDto> NextPatientAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedActiveSessionAsync(sessionId, doctorId);

        // Complete current serving token if any
        var current = await _tokenRepo.GetCurrentServingAsync(sessionId);
        if (current != null)
        {
            current.Status = TokenStatus.Completed;
            current.ServedAt = DateTime.UtcNow;
            await _tokenRepo.UpdateAsync(current);
        }

        var next = await _tokenRepo.GetNextWaitingAsync(sessionId)
            ?? throw new InvalidOperationException("No more patients in queue.");

        next.Status = TokenStatus.Serving;
        await _tokenRepo.UpdateAsync(next);

        session.CurrentTokenNumber = next.TokenNumber;
        await _sessionRepo.UpdateAsync(session);

        var waitingCount = await _tokenRepo.GetWaitingCountAsync(sessionId);
        var doctor = await _doctorRepo.GetByIdAsync(doctorId);

        await _notifier.NotifyQueueAdvancedAsync(doctor!.Slug, next.TokenNumber, waitingCount);

        return MapToDto(next);
    }

    public async Task<TokenDto> SkipPatientAsync(int sessionId, int doctorId)
    {
        var session = await GetOwnedActiveSessionAsync(sessionId, doctorId);

        var current = await _tokenRepo.GetCurrentServingAsync(sessionId)
            ?? throw new InvalidOperationException("No patient is currently being served.");

        current.Status = TokenStatus.Skipped;
        await _tokenRepo.UpdateAsync(current);

        // Move skipped token to end of queue
        await _tokenRepo.RequeueSkippedAsync(current.Id, sessionId);

        // Move to next
        var next = await _tokenRepo.GetNextWaitingAsync(sessionId);

        if (next != null)
        {
            next.Status = TokenStatus.Serving;
            await _tokenRepo.UpdateAsync(next);
            session.CurrentTokenNumber = next.TokenNumber;
            await _sessionRepo.UpdateAsync(session);
        }

        var waitingCount = await _tokenRepo.GetWaitingCountAsync(sessionId);
        var doctor = await _doctorRepo.GetByIdAsync(doctorId);

        await _notifier.NotifyTokenSkippedAsync(
            doctor!.Slug,
            current.TokenNumber,
            next?.TokenNumber ?? session.CurrentTokenNumber
        );

        return MapToDto(current);
    }

    public async Task<TakeTokenResponseDto> AddWalkInAsync(int sessionId, int doctorId, WalkInDto dto)
    {
        var session = await GetOwnedActiveSessionAsync(sessionId, doctorId);
        var waitingCount = await _tokenRepo.GetWaitingCountAsync(sessionId);

        var token = await _tokenRepo.CreateAsync(new Token
        {
            SessionId = sessionId,
            PatientName = dto.PatientName?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Status = TokenStatus.Waiting,
            QueueOrder = waitingCount + 1,
            PublicTokenId = GeneratePublicTokenId()
        });

        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        await _notifier.NotifyTokenCreatedAsync(doctor!.Slug, token.TokenNumber, waitingCount + 1);

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var trackingUrl = $"https://docom.in/t/{token.PublicTokenId}";
        }

        return new TakeTokenResponseDto(
            TokenId: token.Id,
            TokenNumber: token.TokenNumber,
            CurrentTokenNumber: session.CurrentTokenNumber,
            QueuePosition: waitingCount + 1,
            EstimatedWaitMinutes: (waitingCount + 1) * 5,
            PublicTokenId: token.PublicTokenId
        );
    }

    public async Task<TokenDto?> GetTokenStatusAsync(long tokenId)
    {
        var token = await _tokenRepo.GetByIdAsync(tokenId);
        return token is null ? null : MapToDto(token);
    }

    public async Task<TrackingResolveDto?> ResolveTrackingTokenAsync(string publicTokenId)
    {
        var token = await _tokenRepo.GetByPublicTokenIdAsync(publicTokenId);
        if (token is null) return null;

        var session = await _sessionRepo.GetByIdAsync(token.SessionId);
        if (session is null) return null;

        var doctor = await _doctorRepo.GetByIdAsync(session.DoctorId);
        if (doctor is null) return null;

        return new TrackingResolveDto(doctor.Slug, session.Id, token.TokenNumber);
    }

    private async Task<Session> GetOwnedActiveSessionAsync(int sessionId, int doctorId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.DoctorId != doctorId)
            throw new UnauthorizedAccessException("You don't own this session.");

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Session is not active.");

        return session;
    }

    private static TokenDto MapToDto(Token t) =>
        new(t.Id, t.TokenNumber, t.PatientName, t.PhoneNumber, t.Status.ToString(), t.QueueOrder, t.CreatedAt);
}
