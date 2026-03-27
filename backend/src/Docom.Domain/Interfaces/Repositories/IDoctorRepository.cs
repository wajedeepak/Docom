using Docom.Domain.Entities;

namespace Docom.Domain.Interfaces.Repositories;

public interface IDoctorRepository
{
    Task<Doctor?> GetBySlugAsync(string slug);
    Task<Doctor?> GetByIdAsync(int id);
    Task<Doctor?> GetByUserIdAsync(int userId);
    Task<(IEnumerable<Doctor> Items, int Total)> GetAllAsync(int page, int pageSize);
    Task<Doctor> CreateAsync(Doctor doctor);
    Task UpdateAsync(Doctor doctor);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}
