using Docom.Application.DTOs.Token;
using Docom.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Docom.API.Controllers;

[ApiController]
public class TrackingController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TrackingController(ITokenService tokenService) => _tokenService = tokenService;

    /// <summary>
    /// Public: Resolve a tracking token ID to doctor slug + session + token number.
    /// Used by the frontend /t/:id route to restore token context.
    /// </summary>
    [HttpGet("/api/t/{publicTokenId}")]
    [ProducesResponseType(typeof(TrackingResolveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(string publicTokenId)
    {
        var result = await _tokenService.ResolveTrackingTokenAsync(publicTokenId);
        return result is null ? NotFound() : Ok(result);
    }
}
