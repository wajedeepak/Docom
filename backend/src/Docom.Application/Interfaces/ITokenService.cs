using Docom.Application.DTOs.Token;

namespace Docom.Application.Interfaces;

public interface ITokenService
{
    Task<TakeTokenResponseDto> TakeTokenAsync(string doctorSlug, TakeTokenDto dto);
    Task<TokenDto> NextPatientAsync(int sessionId, int doctorId);
    Task<TokenDto> SkipPatientAsync(int sessionId, int doctorId);
    Task<TakeTokenResponseDto> AddWalkInAsync(int sessionId, int doctorId, WalkInDto dto);
    Task<TokenDto?> GetTokenStatusAsync(long tokenId);
    Task<TrackingResolveDto?> ResolveTrackingTokenAsync(string publicTokenId);
}
