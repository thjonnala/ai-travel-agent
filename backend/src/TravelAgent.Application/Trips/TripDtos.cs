using System.ComponentModel.DataAnnotations;
using TravelAgent.Domain;

namespace TravelAgent.Application.Trips;

// ---- Responses ----

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record TripSummaryDto(
    Guid Id,
    string Title,
    string Destination,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TripStatus Status,
    decimal EstimatedTotalCost,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TripDetailDto(
    Guid Id,
    string Title,
    string Destination,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TripStatus Status,
    decimal EstimatedTotalCost,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ItineraryDayDto> Days,
    IReadOnlyList<ChatMessageDto> ChatMessages);

public sealed record ItineraryDayDto(
    Guid Id,
    int DayNumber,
    DateOnly? Date,
    string? Summary,
    decimal EstimatedDayCost,
    IReadOnlyList<ItineraryItemDto> Items);

public sealed record ItineraryItemDto(
    Guid Id,
    TimeBlock TimeBlock,
    ItineraryItemType Type,
    int SortOrder,
    string Title,
    string? Description,
    decimal EstimatedCost,
    string? LocationName,
    double? Lat,
    double? Lng);

public sealed record ChatMessageDto(Guid Id, ChatRole Role, string Content, DateTimeOffset CreatedAt);

// ---- Requests ----

public sealed class PlanTripRequest
{
    /// <summary>Free-text trip description or refinement instruction.</summary>
    [Required, MinLength(3), MaxLength(4000)]
    public required string Request { get; set; }

    /// <summary>When set, refine this existing trip instead of creating a new one.</summary>
    public Guid? TripId { get; set; }
}

public sealed class ChatRequest
{
    [Required, MinLength(1), MaxLength(4000)]
    public required string Message { get; set; }
}

public sealed class UpdateTripRequest
{
    [Required, MaxLength(200)]
    public required string Title { get; set; }

    [Required, MaxLength(200)]
    public required string Destination { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TripStatus Status { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public List<UpdateDayRequest> Days { get; set; } = [];
}

public sealed class UpdateDayRequest
{
    public DateOnly? Date { get; set; }

    [MaxLength(1000)]
    public string? Summary { get; set; }

    public List<UpdateItemRequest> Items { get; set; } = [];
}

public sealed class UpdateItemRequest
{
    public TimeBlock TimeBlock { get; set; }
    public ItineraryItemType Type { get; set; }

    [Required, MaxLength(300)]
    public required string Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0, 1_000_000)]
    public decimal EstimatedCost { get; set; }

    [MaxLength(300)]
    public string? LocationName { get; set; }

    public double? Lat { get; set; }
    public double? Lng { get; set; }
}
