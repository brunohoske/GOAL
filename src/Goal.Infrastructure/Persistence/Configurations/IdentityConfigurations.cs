using Goal.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Goal.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Email).HasColumnType("citext").IsRequired();
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.DisplayName).IsRequired();
        e.Property(x => x.PasswordHash).IsRequired();

        e.HasMany(x => x.DeviceTokens).WithOne(x => x.User!).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.RefreshTokens).WithOne(x => x.User!).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> e)
    {
        e.ToTable("device_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.FcmToken).IsRequired();
        e.HasIndex(x => x.FcmToken);
        e.HasIndex(x => new { x.UserId, x.IsActive });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("refresh_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.TokenHash).IsRequired();
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.Ignore(x => x.IsActive);
    }
}
