using Docom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Docom.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
