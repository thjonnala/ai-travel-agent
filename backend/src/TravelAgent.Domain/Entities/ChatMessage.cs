namespace TravelAgent.Domain.Entities;

/// <summary>
/// One turn of the planning conversation for a trip. Persisted so refinement
/// requests can be replayed to the AI with full context.
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public ChatRole Role { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
