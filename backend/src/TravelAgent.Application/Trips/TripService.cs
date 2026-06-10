using Microsoft.EntityFrameworkCore;
using TravelAgent.Application.Common;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Application.Trips;

public interface ITripService
{
    Task<PagedResult<TripSummaryDto>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TripDetailDto?> GetAsync(Guid userId, Guid tripId, CancellationToken cancellationToken = default);
    Task<TripDetailDto?> UpdateAsync(Guid userId, Guid tripId, UpdateTripRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid userId, Guid tripId, CancellationToken cancellationToken = default);
    Task<TripDetailDto?> DuplicateAsync(Guid userId, Guid tripId, CancellationToken cancellationToken = default);
}

public sealed class TripService(IApplicationDbContext db) : ITripService
{
    public async Task<PagedResult<TripSummaryDto>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Trips.AsNoTracking().Where(t => t.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);
        var trips = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TripSummaryDto>([.. trips.Select(t => t.ToSummaryDto())], page, pageSize, totalCount);
    }

    public async Task<TripDetailDto?> GetAsync(Guid userId, Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await LoadTripAsync(userId, tripId, includeChat: true, track: false, cancellationToken);
        return trip?.ToDetailDto();
    }

    public async Task<TripDetailDto?> UpdateAsync(Guid userId, Guid tripId, UpdateTripRequest request, CancellationToken cancellationToken = default)
    {
        var trip = await LoadTripAsync(userId, tripId, includeChat: true, track: true, cancellationToken);
        if (trip is null) return null;

        trip.Title = request.Title;
        trip.Destination = request.Destination;
        trip.StartDate = request.StartDate;
        trip.EndDate = request.EndDate;
        trip.Status = request.Status;
        trip.Currency = string.IsNullOrWhiteSpace(request.Currency) ? trip.Currency : request.Currency.ToUpperInvariant();
        trip.UpdatedAt = DateTimeOffset.UtcNow;

        // Manual edits send the full itinerary back; replace wholesale.
        db.ItineraryDays.RemoveRange(trip.Days);
        trip.Days.Clear();

        var dayNumber = 1;
        foreach (var dayRequest in request.Days)
        {
            var day = new ItineraryDay
            {
                DayNumber = dayNumber++,
                Date = dayRequest.Date,
                Summary = dayRequest.Summary,
            };

            var sortOrder = 0;
            foreach (var itemRequest in dayRequest.Items)
            {
                day.Items.Add(new ItineraryItem
                {
                    TimeBlock = itemRequest.TimeBlock,
                    Type = itemRequest.Type,
                    SortOrder = sortOrder++,
                    Title = itemRequest.Title,
                    Description = itemRequest.Description,
                    EstimatedCost = itemRequest.EstimatedCost,
                    LocationName = itemRequest.LocationName,
                    Lat = itemRequest.Lat,
                    Lng = itemRequest.Lng,
                });
            }

            day.EstimatedDayCost = day.Items.Sum(i => i.EstimatedCost);
            trip.Days.Add(day);
        }

        trip.EstimatedTotalCost = trip.Days.Sum(d => d.EstimatedDayCost);

        await db.SaveChangesAsync(cancellationToken);
        return trip.ToDetailDto();
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId, cancellationToken);
        if (trip is null) return false;

        db.Trips.Remove(trip);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TripDetailDto?> DuplicateAsync(Guid userId, Guid tripId, CancellationToken cancellationToken = default)
    {
        var source = await LoadTripAsync(userId, tripId, includeChat: false, track: false, cancellationToken);
        if (source is null) return null;

        var now = DateTimeOffset.UtcNow;
        var copy = new Trip
        {
            UserId = userId,
            Title = $"{source.Title} (copy)",
            Destination = source.Destination,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            EstimatedTotalCost = source.EstimatedTotalCost,
            Currency = source.Currency,
            CreatedAt = now,
            UpdatedAt = now,
            Days = [.. source.Days.Select(d => new ItineraryDay
            {
                DayNumber = d.DayNumber,
                Date = d.Date,
                Summary = d.Summary,
                EstimatedDayCost = d.EstimatedDayCost,
                Items = [.. d.Items.Select(i => new ItineraryItem
                {
                    TimeBlock = i.TimeBlock,
                    Type = i.Type,
                    SortOrder = i.SortOrder,
                    Title = i.Title,
                    Description = i.Description,
                    EstimatedCost = i.EstimatedCost,
                    LocationName = i.LocationName,
                    Lat = i.Lat,
                    Lng = i.Lng,
                })],
            })],
        };

        db.Trips.Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        return copy.ToDetailDto();
    }

    private async Task<Trip?> LoadTripAsync(Guid userId, Guid tripId, bool includeChat, bool track, CancellationToken cancellationToken)
    {
        IQueryable<Trip> query = db.Trips
            .Include(t => t.Days).ThenInclude(d => d.Items);
        if (includeChat) query = query.Include(t => t.ChatMessages);
        if (!track) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId, cancellationToken);
    }
}
