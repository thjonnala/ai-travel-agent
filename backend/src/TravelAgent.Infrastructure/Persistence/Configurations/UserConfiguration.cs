using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.ExternalAuthId).HasMaxLength(128);
        builder.Property(u => u.Email).HasMaxLength(320);
        builder.Property(u => u.DisplayName).HasMaxLength(200);

        // Lookup by identity-provider subject happens on every request.
        builder.HasIndex(u => u.ExternalAuthId).IsUnique();

        builder.HasOne(u => u.Preference)
            .WithOne(p => p.User)
            .HasForeignKey<TravelerPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
