using Microsoft.AspNetCore.Mvc;
using TravelAgent.Application.Common;
using TravelAgent.Application.Preferences;

namespace TravelAgent.Api.Controllers;

[ApiController]
[Route("api/preferences")]
public class PreferencesController(IPreferenceService preferences, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>The current user's traveler preferences (defaults when never saved).</summary>
    [HttpGet]
    public async Task<ActionResult<PreferenceDto>> Get(CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        return Ok(await preferences.GetAsync(userId, cancellationToken));
    }

    /// <summary>Create or update the current user's traveler preferences.</summary>
    [HttpPut]
    public async Task<ActionResult<PreferenceDto>> Update(UpdatePreferenceRequest request, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        return Ok(await preferences.UpdateAsync(userId, request, cancellationToken));
    }
}
