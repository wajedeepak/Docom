using Docom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docom.Infrastructure.Data.Configurations;

public class TokenConfiguration : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.PatientName).HasMaxLength(150);
        builder.Property(t => t.PhoneNumber).HasMaxLength(20);
        builder.Property(t => t.PublicTokenId).HasMaxLength(10).IsRequired();
        builder.HasIndex(t => t.PublicTokenId).IsUnique();
        builder.Property(t => t.Status).IsRequired();

        // Critical: queue reads filter by SessionId + Status ordered by QueueOrder
        builder.HasIndex(t => new { t.SessionId, t.Status, t.QueueOrder });

        // Unique constraint: one token number per session
        builder.HasIndex(t => new { t.SessionId, t.TokenNumber }).IsUnique();
    }
}
