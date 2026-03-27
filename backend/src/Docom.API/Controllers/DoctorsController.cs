using Docom.Application.DTOs.Session;
using Docom.Application.DTOs.Token;
using Docom.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Docom.API.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ITokenService _tokenService;

    public DoctorsController(ISessionService sessionService, ITokenService tokenService)
    {
        _sessionService = sessionService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Public: Get doctor info and current queue state by slug.
    /// This is the primary read for the patient-facing page at docom.in/drslug.
    /// </summary>
    [HttpGet("{slug}/queue")]
    [ProducesResponseType(typeof(PublicQueueStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicQueue(string slug)
    {
        var state = await _sessionService.GetPublicQueueStateAsync(slug);
        return Ok(state);
    }

    /// <summary>
    /// Public: Take a token for a doctor's queue. No login required.
    /// </summary>
    [HttpPost("{slug}/tokens")]
    [ProducesResponseType(typeof(TakeTokenResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TakeToken(string slug, [FromBody] TakeTokenDto dto)
    {
        var result = await _tokenService.TakeTokenAsync(slug, dto);
        return CreatedAtAction(nameof(GetPublicQueue), new { slug }, result);
    }
}
