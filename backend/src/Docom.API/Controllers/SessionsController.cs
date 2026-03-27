using Docom.Application.DTOs.Session;
using Docom.Application.Interfaces;
using Docom.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docom.API.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize(Roles = "Doctor,Receptionist,Admin")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService) => _sessionService = sessionService;

    [HttpPost]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSessionDto dto)
    {
        var doctorId = User.GetDoctorId();
        var session = await _sessionService.CreateSessionAsync(doctorId, dto);
        return CreatedAtAction(nameof(GetQueueState), new { sessionId = session.Id }, session);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        var doctorId = User.GetDoctorId();
        var session = await _sessionService.GetActiveSessionAsync(doctorId);
        return session is null ? NoContent() : Ok(session);
    }

    [HttpPost("{sessionId}/start")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(int sessionId)
    {
        var session = await _sessionService.StartSessionAsync(sessionId, User.GetDoctorId());
        return Ok(session);
    }

    [HttpPost("{sessionId}/pause")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pause(int sessionId)
    {
        var session = await _sessionService.PauseSessionAsync(sessionId, User.GetDoctorId());
        return Ok(session);
    }

    [HttpPost("{sessionId}/resume")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resume(int sessionId)
    {
        var session = await _sessionService.ResumeSessionAsync(sessionId, User.GetDoctorId());
        return Ok(session);
    }

    [HttpPost("{sessionId}/end")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> End(int sessionId)
    {
        var session = await _sessionService.EndSessionAsync(sessionId, User.GetDoctorId());
        return Ok(session);
    }

    [HttpGet("{sessionId}/queue")]
    [ProducesResponseType(typeof(QueueStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueState(int sessionId)
    {
        var state = await _sessionService.GetQueueStateAsync(sessionId, User.GetDoctorId());
        return Ok(state);
    }
}
