using Docom.Application.DTOs.Common;
using Docom.Application.DTOs.Doctor;
using Docom.Application.Interfaces;
using Docom.Domain.Entities;
using Docom.Domain.Enums;
using Docom.Domain.Interfaces.Repositories;

namespace Docom.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepo;
    private readonly IUserRepository _userRepo;

    public DoctorService(IDoctorRepository doctorRepo, IUserRepository userRepo)
    {
        _doctorRepo = doctorRepo;
        _userRepo = userRepo;
    }

    public async Task<DoctorDto?> GetBySlugAsync(string slug)
    {
        var doctor = await _doctorRepo.GetBySlugAsync(slug);
        return doctor is null ? null : MapToDto(doctor);
    }

    public async Task<DoctorDto?> GetByIdAsync(int id)
    {
        var doctor = await _doctorRepo.GetByIdAsync(id);
        return doctor is null ? null : MapToDto(doctor);
    }

    public async Task<PagedResult<DoctorDto>> GetAllAsync(int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _doctorRepo.GetAllAsync(page, pageSize);
        return new PagedResult<DoctorDto>(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<DoctorDto> CreateAsync(CreateDoctorDto dto)
    {
        if (await _doctorRepo.SlugExistsAsync(dto.Slug))
            throw new InvalidOperationException($"Slug '{dto.Slug}' is already taken.");

        var existingUser = await _userRepo.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = await _userRepo.CreateAsync(new User
        {
            Email = dto.Email,
            Name = dto.Name,
            Role = UserRole.Doctor
        });

        var doctor = await _doctorRepo.CreateAsync(new Doctor
        {
            Name = dto.Name,
            Specialization = dto.Specialization,
            Slug = dto.Slug.ToLowerInvariant(),
            Address = dto.Address,
            Pincode = dto.Pincode,
            UserId = user.Id
        });

        return MapToDto(doctor);
    }

    public async Task<DoctorDto> UpdateAsync(int id, UpdateDoctorDto dto)
    {
        var doctor = await _doctorRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Doctor not found.");

        if (await _doctorRepo.SlugExistsAsync(dto.Slug, excludeId: id))
            throw new InvalidOperationException($"Slug '{dto.Slug}' is already taken.");

        doctor.Name = dto.Name;
        doctor.Specialization = dto.Specialization;
        doctor.Slug = dto.Slug.ToLowerInvariant();
        doctor.Address = dto.Address;
        doctor.Pincode = dto.Pincode;
        doctor.IsActive = dto.IsActive;

        await _doctorRepo.UpdateAsync(doctor);
        return MapToDto(doctor);
    }

    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
        => _doctorRepo.SlugExistsAsync(slug, excludeId);

    private static DoctorDto MapToDto(Doctor d) =>
        new(d.Id, d.Slug, d.Name, d.Specialization, d.Address, d.Pincode, d.IsActive, d.CreatedAt);
}
