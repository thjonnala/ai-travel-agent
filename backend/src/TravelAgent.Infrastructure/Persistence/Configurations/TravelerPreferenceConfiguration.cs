using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Infrastructure.Persistence.Configurations;

public class TravelerPreferenceConfiguration : IEntityTypeConfiguration<TravelerPreference>
{
    public void Configure(EntityTypeBuilder<TravelerPreference> builder)
    {
        builder.Property(p => p.BudgetBand).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Pace).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Interests).HasMaxLength(2000);
        builder.Property(p => p.DietaryNeeds).HasMaxLength(500);
        builder.Property(p => p.Accessibility).HasMaxLength(500);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
