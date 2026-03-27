using Docom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docom.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Slug).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Specialization).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Address).HasMaxLength(300);
        builder.Property(d => d.Pincode).HasMaxLength(10);

        // Critical: slug lookup is the primary hot path for patient pages
        builder.HasIndex(d => d.Slug).IsUnique();
        builder.HasIndex(d => d.IsActive);

        builder.HasMany(d => d.Sessions)
               .WithOne(s => s.Doctor)
               .HasForeignKey(s => s.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
