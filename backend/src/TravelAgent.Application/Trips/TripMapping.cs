using TravelAgent.Application.Planning;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Application.Trips;

internal static class TripMapping
{
    public static TripSummaryDto ToSummaryDto(this Trip trip) => new(
        trip.Id, trip.Title, trip.Destination, trip.StartDate, trip.EndDate,
        trip.Status, trip.EstimatedTotalCost, trip.Currency, trip.CreatedAt, trip.UpdatedAt);

    public static TripDetailDto ToDetailDto(this Trip trip) => new(
        trip.Id, trip.Title, trip.Destination, trip.StartDate, trip.EndDate,
        trip.Status, trip.EstimatedTotalCost, trip.Currency, trip.CreatedAt, trip.UpdatedAt,
        [.. trip.Days
            .OrderBy(d => d.DayNumber)
            .Select(d => new ItineraryDayDto(
                d.Id, d.DayNumber, d.Date, d.Summary, d.EstimatedDayCost,
                [.. d.Items
                    .OrderBy(i => i.TimeBlock).ThenBy(i => i.SortOrder)
                    .Select(i => new ItineraryItemDto(
                        i.Id, i.TimeBlock, i.Type, i.SortOrder, i.Title, i.Description,
                        i.EstimatedCost, i.LocationName, i.Lat, i.Lng))]))],
        [.. trip.ChatMessages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAt))]);

    /// <summary>Snapshot of a persisted trip in the draft shape the AI consumes for refinements.</summary>
    public static ItineraryDraft ToDraft(this Trip trip) => new()
    {
        Title = trip.Title,
        Destination = trip.Destination,
        StartDate = trip.StartDate,
        EndDate = trip.EndDate,
        Currency = trip.Currency,
        Days = [.. trip.Days
            .OrderBy(d => d.DayNumber)
            .Select(d => new ItineraryDayDraft
            {
                DayNumber = d.DayNumber,
                Date = d.Date,
                Summary = d.Summary,
                Items = [.. d.Items
                    .OrderBy(i => i.TimeBlock).ThenBy(i => i.SortOrder)
                    .Select(i => new ItineraryItemDraft
                    {
                        TimeBlock = i.TimeBlock,
                        Type = i.Type,
                        Title = i.Title,
                        Description = i.Description,
                        EstimatedCost = i.EstimatedCost,
                        LocationName = i.LocationName,
                    })],
            })],
    };
}
