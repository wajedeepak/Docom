using Docom.Domain.Entities;
using Docom.Domain.Interfaces.Repositories;
using Docom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Docom.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _db;

    public DoctorRepository(AppDbContext db) => _db = db;

    public Task<Doctor?> GetBySlugAsync(string slug)
        => _db.Doctors
              .AsNoTracking()
              .FirstOrDefaultAsync(d => d.Slug == slug && d.IsActive);

    public Task<Doctor?> GetByIdAsync(int id)
        => _db.Doctors
              .AsNoTracking()
              .FirstOrDefaultAsync(d => d.Id == id);

    public Task<Doctor?> GetByUserIdAsync(int userId)
        => _db.Doctors
              .AsNoTracking()
              .FirstOrDefaultAsync(d => d.UserId == userId);

    public async Task<(IEnumerable<Doctor> Items, int Total)> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Doctors.AsNoTracking().OrderBy(d => d.Name);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Doctor> CreateAsync(Doctor doctor)
    {
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return doctor;
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        _db.Doctors.Update(doctor);
        await _db.SaveChangesAsync();
    }

    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var query = _db.Doctors.Where(d => d.Slug == slug);
        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);
        return query.AnyAsync();
    }
}
