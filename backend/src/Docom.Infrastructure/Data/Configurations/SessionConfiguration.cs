using Docom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docom.Infrastructure.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Label).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Status).IsRequired();

        // Critical: find active session for a doctor quickly
        builder.HasIndex(s => new { s.DoctorId, s.Status });

        builder.HasMany(s => s.Tokens)
               .WithOne(t => t.Session)
               .HasForeignKey(t => t.SessionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
