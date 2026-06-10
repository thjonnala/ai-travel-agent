namespace TravelAgent.Domain.Entities;

/// <summary>
/// An application user, provisioned on first authenticated request and keyed
/// to the Entra ID subject claim via <see cref="ExternalAuthId"/>.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>The immutable subject (oid/sub) claim from the identity provider.</summary>
    public required string ExternalAuthId { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public TravelerPreference? Preference { get; set; }

    public ICollection<Trip> Trips { get; set; } = [];
}
