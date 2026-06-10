using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Infrastructure.Persistence.Configurations;

public class ItineraryItemConfiguration : IEntityTypeConfiguration<ItineraryItem>
{
    public void Configure(EntityTypeBuilder<ItineraryItem> builder)
    {
        builder.Property(i => i.TimeBlock).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Title).HasMaxLength(300);
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.EstimatedCost).HasPrecision(18, 2);
        builder.Property(i => i.LocationName).HasMaxLength(300);

        builder.HasIndex(i => i.ItineraryDayId);
    }
}
