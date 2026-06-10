namespace TravelAgent.Application.Common;

/// <summary>
/// Resolves the current user's database id. The demo implementation returns a
/// fixed local user; the Entra ID implementation (milestone 2) will resolve it
/// from the JWT subject claim. All queries must be scoped through this.
/// </summary>
public interface ICurrentUserService
{
    Task<Guid> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
}
