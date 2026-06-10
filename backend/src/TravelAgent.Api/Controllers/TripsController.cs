using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgent.Application.Common;
using TravelAgent.Application.Planning;
using TravelAgent.Application.Trips;

namespace TravelAgent.Api.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController(
    ITripService trips,
    ITripPlanningService planning,
    ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Plan a new trip from a free-text request, or refine an existing one when tripId is set.</summary>
    [HttpPost("plan")]
    [EnableRateLimiting("ai")]
    [ProducesResponseType<TripDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TripDetailDto>> Plan(PlanTripRequest request, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        try
        {
            var trip = await planning.PlanAsync(userId, request, cancellationToken);
            return trip is null ? NotFound() : Ok(trip);
        }
        catch (AiPlannerException ex)
        {
            return Problem(title: "AI planning failed", detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Send a refinement message for an existing trip's itinerary.</summary>
    [HttpPost("{id:guid}/chat")]
    [EnableRateLimiting("ai")]
    [ProducesResponseType<TripDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public Task<ActionResult<TripDetailDto>> Chat(Guid id, ChatRequest request, CancellationToken cancellationToken) =>
        Plan(new PlanTripRequest { Request = request.Message, TripId = id }, cancellationToken);

    /// <summary>List the current user's trips, most recently updated first.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TripSummaryDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        return Ok(await trips.ListAsync(userId, page, pageSize, cancellationToken));
    }

    /// <summary>Full trip with days, items, and chat history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TripDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        var trip = await trips.GetAsync(userId, id, cancellationToken);
        return trip is null ? NotFound() : Ok(trip);
    }

    /// <summary>Apply manual edits to a trip's metadata and itinerary.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TripDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripDetailDto>> Update(Guid id, UpdateTripRequest request, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        var trip = await trips.UpdateAsync(userId, id, request, cancellationToken);
        return trip is null ? NotFound() : Ok(trip);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        return await trips.DeleteAsync(userId, id, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>Create a copy of a trip (itinerary only, chat history not carried over).</summary>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType<TripDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripDetailDto>> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetCurrentUserIdAsync(cancellationToken);
        var copy = await trips.DuplicateAsync(userId, id, cancellationToken);
        return copy is null ? NotFound() : CreatedAtAction(nameof(Get), new { id = copy.Id }, copy);
    }
}
