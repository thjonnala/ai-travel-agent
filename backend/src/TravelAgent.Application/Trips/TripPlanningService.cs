using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgent.Application.Common;
using TravelAgent.Application.Planning;
using TravelAgent.Domain;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Application.Trips;

public interface ITripPlanningService
{
    /// <summary>
    /// Plans a new trip, or refines an existing one when <see cref="PlanTripRequest.TripId"/>
    /// is set. Returns null when the referenced trip doesn't belong to the user.
    /// </summary>
    Task<TripDetailDto?> PlanAsync(Guid userId, PlanTripRequest request, CancellationToken cancellationToken = default);
}

public sealed class TripPlanningService(IApplicationDbContext db, IAiPlannerService planner) : ITripPlanningService
{
    /// <summary>Most recent chat turns replayed to the model for refinement context.</summary>
    private const int HistoryWindow = 12;

    public async Task<TripDetailDto?> PlanAsync(Guid userId, PlanTripRequest request, CancellationToken cancellationToken = default)
    {
        Trip? trip = null;
        if (request.TripId is { } tripId)
        {
            trip = await db.Trips
                .Include(t => t.Days).ThenInclude(d => d.Items)
                .Include(t => t.ChatMessages)
                .FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId, cancellationToken);
            if (trip is null) return null;
        }

        var context = new PlanningContext(
            request.Request,
            await GetPreferenceInfoAsync(userId, cancellationToken),
            trip?.ToDraft(),
            trip is null
                ? []
                : [.. trip.ChatMessages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(HistoryWindow)
                    .Reverse()
                    .Select(m => new ChatTurn(m.Role, m.Content))]);

        var draft = await planner.GeneratePlanAsync(context, cancellationToken);

        var errors = ItineraryValidator.Validate(draft);
        if (errors.Count > 0)
            throw new AiPlannerException($"AI returned an invalid itinerary: {string.Join("; ", errors)}");

        var now = DateTimeOffset.UtcNow;
        if (trip is null)
        {
            trip = new Trip { Title = draft.Title, Destination = draft.Destination, UserId = userId, CreatedAt = now };
            db.Trips.Add(trip);
        }
        else
        {
            // Full replace keeps persistence simple: the draft is always the
            // complete itinerary, not a diff.
            db.ItineraryDays.RemoveRange(trip.Days);
            trip.Days.Clear();
        }

        ApplyDraft(trip, draft, now);

        trip.ChatMessages.Add(new ChatMessage { Role = ChatRole.User, Content = request.Request, CreatedAt = now });
        trip.ChatMessages.Add(new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = string.IsNullOrWhiteSpace(draft.AssistantMessage)
                ? "I've updated your itinerary."
                : draft.AssistantMessage,
            // Nudge ordering so the reply always renders after the user turn.
            CreatedAt = now.AddMilliseconds(1),
        });

        await db.SaveChangesAsync(cancellationToken);
        return trip.ToDetailDto();
    }

    private static void ApplyDraft(Trip trip, ItineraryDraft draft, DateTimeOffset now)
    {
        trip.Title = draft.Title;
        trip.Destination = draft.Destination;
        trip.StartDate = draft.StartDate;
        trip.EndDate = draft.EndDate;
        trip.Currency = draft.Currency;
        trip.Status = TripStatus.Planned;
        trip.UpdatedAt = now;

        foreach (var dayDraft in draft.Days)
        {
            var day = new ItineraryDay
            {
                DayNumber = dayDraft.DayNumber,
                Date = dayDraft.Date,
                Summary = dayDraft.Summary,
            };

            var sortOrder = 0;
            foreach (var itemDraft in dayDraft.Items)
            {
                day.Items.Add(new ItineraryItem
                {
                    TimeBlock = itemDraft.TimeBlock,
                    Type = itemDraft.Type,
                    SortOrder = sortOrder++,
                    Title = itemDraft.Title,
                    Description = itemDraft.Description,
                    EstimatedCost = itemDraft.EstimatedCost,
                    LocationName = itemDraft.LocationName,
                });
            }

            // Costs are derived server-side rather than trusted from the model.
            day.EstimatedDayCost = day.Items.Sum(i => i.EstimatedCost);
            trip.Days.Add(day);
        }

        trip.EstimatedTotalCost = trip.Days.Sum(d => d.EstimatedDayCost);
    }

    private async Task<TravelerPreferenceInfo?> GetPreferenceInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await db.TravelerPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (preference is null) return null;

        List<string> interests;
        try
        {
            interests = JsonSerializer.Deserialize<List<string>>(preference.Interests) ?? [];
        }
        catch (JsonException)
        {
            interests = [];
        }

        return new TravelerPreferenceInfo(
            preference.BudgetBand, preference.Pace, interests,
            preference.DietaryNeeds, preference.Accessibility);
    }
}
