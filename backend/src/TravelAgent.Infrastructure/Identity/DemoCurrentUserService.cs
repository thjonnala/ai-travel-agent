using Microsoft.EntityFrameworkCore;
using TravelAgent.Application.Common;
using TravelAgent.Domain.Entities;
using TravelAgent.Infrastructure.Persistence;

namespace TravelAgent.Infrastructure.Identity;

/// <summary>
/// Demo-mode identity: every request acts as one fixed local user, which is
/// provisioned on first use. Swapped for a JWT-claims-based implementation
/// when Entra ID auth lands. Query scoping by user id stays identical.
/// </summary>
public sealed class DemoCurrentUserService(TravelAgentDbContext db) : ICurrentUserService
{
    private const string DemoExternalAuthId = "demo|local-user";

    public async Task<Guid> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == DemoExternalAuthId, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                ExternalAuthId = DemoExternalAuthId,
                Email = "demo@example.com",
                DisplayName = "Demo Traveler",
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Lost a first-request race on the unique ExternalAuthId index;
                // the row exists now, so read it back.
                db.Users.Entry(user).State = EntityState.Detached;
                user = await db.Users.SingleAsync(u => u.ExternalAuthId == DemoExternalAuthId, cancellationToken);
            }
        }

        return user.Id;
    }
}
