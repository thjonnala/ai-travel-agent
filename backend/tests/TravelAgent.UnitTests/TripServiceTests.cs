using TravelAgent.Application.Trips;
using TravelAgent.Domain;
using TravelAgent.Domain.Entities;

namespace TravelAgent.UnitTests;

public class TripServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly TripService _service;
    private readonly Guid _userId;
    private readonly Guid _otherUserId;

    public TripServiceTests()
    {
        var user = new User { ExternalAuthId = "test", Email = "t@example.com", DisplayName = "Test" };
        var other = new User { ExternalAuthId = "other", Email = "o@example.com", DisplayName = "Other" };
        _db.Context.Users.AddRange(user, other);
        _db.Context.SaveChanges();
        _userId = user.Id;
        _otherUserId = other.Id;

        _service = new TripService(_db.Context);
    }

    public void Dispose() => _db.Dispose();

    private Trip SeedTrip(string title = "Lisbon trip", Guid? ownerId = null, DateTimeOffset? updatedAt = null)
    {
        var trip = new Trip
        {
            UserId = ownerId ?? _userId,
            Title = title,
            Destination = "Lisbon",
            Status = TripStatus.Planned,
            Currency = "USD",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow,
            EstimatedTotalCost = 130,
            Days =
            [
                new ItineraryDay
                {
                    DayNumber = 1,
                    Summary = "Old town",
                    EstimatedDayCost = 130,
                    Items =
                    [
                        new ItineraryItem { TimeBlock = TimeBlock.Evening, Type = ItineraryItemType.Lodging, SortOrder = 0, Title = "Hotel", EstimatedCost = 120 },
                        new ItineraryItem { TimeBlock = TimeBlock.Morning, Type = ItineraryItemType.Activity, SortOrder = 0, Title = "Walk", EstimatedCost = 10 },
                    ],
                },
            ],
            ChatMessages = [new ChatMessage { Role = ChatRole.User, Content = "plan it", CreatedAt = DateTimeOffset.UtcNow }],
        };
        _db.Context.Trips.Add(trip);
        _db.Context.SaveChanges();
        return trip;
    }

    [Fact]
    public async Task List_pages_and_orders_by_most_recently_updated()
    {
        SeedTrip("oldest", updatedAt: DateTimeOffset.UtcNow.AddDays(-2));
        SeedTrip("newest", updatedAt: DateTimeOffset.UtcNow);
        SeedTrip("middle", updatedAt: DateTimeOffset.UtcNow.AddDays(-1));

        var page1 = await _service.ListAsync(_userId, page: 1, pageSize: 2);
        var page2 = await _service.ListAsync(_userId, page: 2, pageSize: 2);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(["newest", "middle"], page1.Items.Select(t => t.Title));
        Assert.Equal(["oldest"], page2.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task List_clamps_page_and_page_size_and_scopes_to_user()
    {
        SeedTrip("mine");
        SeedTrip("theirs", ownerId: _otherUserId);

        var result = await _service.ListAsync(_userId, page: 0, pageSize: 9999);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal("mine", result.Items.Single().Title);
    }

    [Fact]
    public async Task Get_returns_full_detail_with_items_ordered_by_time_block()
    {
        var trip = SeedTrip();

        var detail = await _service.GetAsync(_userId, trip.Id);

        Assert.NotNull(detail);
        Assert.Single(detail.Days);
        // Morning item first even though it was stored second.
        Assert.Equal(["Walk", "Hotel"], detail.Days[0].Items.Select(i => i.Title));
        Assert.Single(detail.ChatMessages);
    }

    [Fact]
    public async Task Get_returns_null_for_unknown_or_foreign_trips()
    {
        var trip = SeedTrip();

        Assert.Null(await _service.GetAsync(_userId, Guid.NewGuid()));
        Assert.Null(await _service.GetAsync(_otherUserId, trip.Id));
    }

    [Fact]
    public async Task Update_replaces_itinerary_recomputes_costs_and_pins_usd()
    {
        var trip = SeedTrip();

        var updated = await _service.UpdateAsync(_userId, trip.Id, new UpdateTripRequest
        {
            Title = "Lisbon, edited",
            Destination = "Lisbon, Portugal",
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 2),
            Status = TripStatus.Archived,
            Currency = "eur", // ignored: USD-only product
            Days =
            [
                new UpdateDayRequest
                {
                    Date = new DateOnly(2026, 10, 1),
                    Summary = "New day 1",
                    Items =
                    [
                        new UpdateItemRequest { TimeBlock = TimeBlock.Morning, Type = ItineraryItemType.Activity, Title = "Museum", EstimatedCost = 25, LocationName = "Belém", Lat = 38.7, Lng = -9.2 },
                        new UpdateItemRequest { TimeBlock = TimeBlock.Evening, Type = ItineraryItemType.Dining, Title = "Dinner", EstimatedCost = 40 },
                    ],
                },
                new UpdateDayRequest { Summary = "New day 2", Items = [new UpdateItemRequest { TimeBlock = TimeBlock.Morning, Type = ItineraryItemType.Transport, Title = "Train", EstimatedCost = 15 }] },
            ],
        });

        Assert.NotNull(updated);
        Assert.Equal("Lisbon, edited", updated.Title);
        Assert.Equal(TripStatus.Archived, updated.Status);
        Assert.Equal("USD", updated.Currency);
        Assert.Equal(2, updated.Days.Count);
        Assert.Equal(65, updated.Days[0].EstimatedDayCost);
        Assert.Equal(15, updated.Days[1].EstimatedDayCost);
        Assert.Equal(80, updated.EstimatedTotalCost);
        // Old itinerary rows are gone, not orphaned.
        Assert.Equal(2, _db.Context.ItineraryDays.Count());
    }

    [Fact]
    public async Task Update_returns_null_for_foreign_trip()
    {
        var trip = SeedTrip();

        var result = await _service.UpdateAsync(_otherUserId, trip.Id, new UpdateTripRequest
        {
            Title = "hijack",
            Destination = "x",
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_removes_trip_and_children()
    {
        var trip = SeedTrip();

        Assert.True(await _service.DeleteAsync(_userId, trip.Id));
        Assert.Empty(_db.Context.Trips);
        Assert.Empty(_db.Context.ItineraryDays);
        Assert.Empty(_db.Context.ChatMessages);
    }

    [Fact]
    public async Task Delete_returns_false_for_foreign_or_unknown_trip()
    {
        var trip = SeedTrip();

        Assert.False(await _service.DeleteAsync(_otherUserId, trip.Id));
        Assert.False(await _service.DeleteAsync(_userId, Guid.NewGuid()));
        Assert.Single(_db.Context.Trips);
    }

    [Fact]
    public async Task Duplicate_copies_itinerary_but_not_chat()
    {
        var trip = SeedTrip();

        var copy = await _service.DuplicateAsync(_userId, trip.Id);

        Assert.NotNull(copy);
        Assert.NotEqual(trip.Id, copy.Id);
        Assert.Equal("Lisbon trip (copy)", copy.Title);
        Assert.Equal(trip.EstimatedTotalCost, copy.EstimatedTotalCost);
        Assert.Single(copy.Days);
        Assert.Equal(2, copy.Days[0].Items.Count);
        Assert.Empty(copy.ChatMessages);
    }

    [Fact]
    public async Task Duplicate_returns_null_for_foreign_trip()
    {
        var trip = SeedTrip();

        Assert.Null(await _service.DuplicateAsync(_otherUserId, trip.Id));
    }
}
