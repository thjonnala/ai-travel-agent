using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
        // Content is unbounded: assistant turns can carry full itinerary JSON.

        builder.HasIndex(m => new { m.TripId, m.CreatedAt });
    }
}
