using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Infrastructure.Persistence.Configurations;

public class ItineraryDayConfiguration : IEntityTypeConfiguration<ItineraryDay>
{
    public void Configure(EntityTypeBuilder<ItineraryDay> builder)
    {
        builder.Property(d => d.Summary).HasMaxLength(1000);
        builder.Property(d => d.EstimatedDayCost).HasPrecision(18, 2);

        builder.HasIndex(d => new { d.TripId, d.DayNumber }).IsUnique();

        builder.HasMany(d => d.Items)
            .WithOne(i => i.ItineraryDay)
            .HasForeignKey(i => i.ItineraryDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
