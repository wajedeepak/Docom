using Docom.Application.DTOs.Token;
using Docom.Application.Interfaces;
using Docom.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docom.API.Controllers;

[ApiController]
[Route("api/sessions/{sessionId}/tokens")]
public class TokensController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokensController(ITokenService tokenService) => _tokenService = tokenService;

    /// <summary>
    /// Doctor/Receptionist: Advance queue to next patient.
    /// </summary>
    [HttpPost("next")]
    [Authorize(Roles = "Doctor,Receptionist,Admin")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Next(int sessionId)
    {
        var token = await _tokenService.NextPatientAsync(sessionId, User.GetDoctorId());
        return Ok(token);
    }

    /// <summary>
    /// Doctor/Receptionist: Skip current patient (moves to end of queue).
    /// </summary>
    [HttpPost("skip")]
    [Authorize(Roles = "Doctor,Receptionist,Admin")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Skip(int sessionId)
    {
        var token = await _tokenService.SkipPatientAsync(sessionId, User.GetDoctorId());
        return Ok(token);
    }

    /// <summary>
    /// Doctor/Receptionist: Add a walk-in patient. Name and phone are optional.
    /// </summary>
    [HttpPost("walkin")]
    [Authorize(Roles = "Doctor,Receptionist,Admin")]
    [ProducesResponseType(typeof(TakeTokenResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> WalkIn(int sessionId, [FromBody] WalkInDto dto)
    {
        var result = await _tokenService.AddWalkInAsync(sessionId, User.GetDoctorId(), dto);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Public: Get status of a specific token (for patient tracking).
    /// </summary>
    [HttpGet("{tokenId}")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(int sessionId, long tokenId)
    {
        var token = await _tokenService.GetTokenStatusAsync(tokenId);
        return token is null ? NotFound() : Ok(token);
    }
}
