using Docom.Application.DTOs.Common;
using Docom.Application.DTOs.Doctor;
using Docom.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docom.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public AdminController(IDoctorService doctorService) => _doctorService = doctorService;

    [HttpGet("doctors")]
    [ProducesResponseType(typeof(PagedResult<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _doctorService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("doctors")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        var doctor = await _doctorService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetDoctor), new { id = doctor.Id }, doctor);
    }

    [HttpGet("doctors/{id}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctor(int id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    [HttpPut("doctors/{id}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDto dto)
    {
        var doctor = await _doctorService.UpdateAsync(id, dto);
        return Ok(doctor);
    }

    /// <summary>
    /// Check if a slug is available.
    /// Pass excludeId when editing an existing doctor so their current slug is not flagged as taken.
    /// </summary>
    [HttpGet("doctors/check-slug")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckSlug([FromQuery] string slug, [FromQuery] int? excludeId = null)
    {
        var exists = await _doctorService.SlugExistsAsync(slug, excludeId);
        return Ok(new { slug, available = !exists });
    }
}
