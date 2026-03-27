using Docom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docom.Infrastructure.Data.Configurations;

public class OtpRequestConfiguration : IEntityTypeConfiguration<OtpRequest>
{
    public void Configure(EntityTypeBuilder<OtpRequest> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Email).HasMaxLength(256).IsRequired();
        builder.Property(o => o.OtpHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(o => new { o.Email, o.IsUsed });
    }
}
