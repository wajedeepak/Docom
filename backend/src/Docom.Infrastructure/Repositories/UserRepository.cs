using Docom.Domain.Entities;
using Docom.Domain.Interfaces.Repositories;
using Docom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Docom.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email)
        => _db.Users
              .Include(u => u.Doctor)
              .AsNoTracking()
              .FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(int id)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }
}
