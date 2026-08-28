using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class OtpRequestThrottleConfiguration : IEntityTypeConfiguration<OtpRequestThrottle>
{
    public void Configure(EntityTypeBuilder<OtpRequestThrottle> builder)
    {
        builder.ToTable("OtpRequestThrottles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmailNormalizado).IsRequired().HasMaxLength(320);
        builder.HasIndex(x => x.EmailNormalizado).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
