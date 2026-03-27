using Docom.Application.DTOs.Common;
using Docom.Application.DTOs.Doctor;

namespace Docom.Application.Interfaces;

public interface IDoctorService
{
    Task<DoctorDto?> GetBySlugAsync(string slug);
    Task<DoctorDto?> GetByIdAsync(int id);
    Task<PagedResult<DoctorDto>> GetAllAsync(int page, int pageSize);
    Task<DoctorDto> CreateAsync(CreateDoctorDto dto);
    Task<DoctorDto> UpdateAsync(int id, UpdateDoctorDto dto);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}
