using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgent.Application.Common;
using TravelAgent.Domain;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Application.Preferences;

public sealed record PreferenceDto(
    BudgetBand BudgetBand,
    TripPace Pace,
    IReadOnlyList<string> Interests,
    string? DietaryNeeds,
    string? Accessibility,
    DateTimeOffset? UpdatedAt);

public sealed class UpdatePreferenceRequest
{
    public BudgetBand BudgetBand { get; set; }
    public TripPace Pace { get; set; }

    [MaxLength(50)]
    public List<string> Interests { get; set; } = [];

    [MaxLength(500)]
    public string? DietaryNeeds { get; set; }

    [MaxLength(500)]
    public string? Accessibility { get; set; }
}

public interface IPreferenceService
{
    /// <summary>Returns stored preferences, or sensible defaults when none exist yet.</summary>
    Task<PreferenceDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PreferenceDto> UpdateAsync(Guid userId, UpdatePreferenceRequest request, CancellationToken cancellationToken = default);
}

public sealed class PreferenceService(IApplicationDbContext db) : IPreferenceService
{
    public async Task<PreferenceDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preference = await db.TravelerPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return preference is null
            ? new PreferenceDto(BudgetBand.MidRange, TripPace.Moderate, [], null, null, null)
            : ToDto(preference);
    }

    public async Task<PreferenceDto> UpdateAsync(Guid userId, UpdatePreferenceRequest request, CancellationToken cancellationToken = default)
    {
        var preference = await db.TravelerPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preference is null)
        {
            preference = new TravelerPreference { UserId = userId };
            db.TravelerPreferences.Add(preference);
        }

        preference.BudgetBand = request.BudgetBand;
        preference.Pace = request.Pace;
        preference.Interests = JsonSerializer.Serialize(
            request.Interests.Select(i => i.Trim()).Where(i => i.Length > 0).Distinct().Take(50));
        preference.DietaryNeeds = request.DietaryNeeds;
        preference.Accessibility = request.Accessibility;
        preference.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(preference);
    }

    private static PreferenceDto ToDto(TravelerPreference preference)
    {
        List<string> interests;
        try
        {
            interests = JsonSerializer.Deserialize<List<string>>(preference.Interests) ?? [];
        }
        catch (JsonException)
        {
            interests = [];
        }

        return new PreferenceDto(
            preference.BudgetBand, preference.Pace, interests,
            preference.DietaryNeeds, preference.Accessibility, preference.UpdatedAt);
    }
}
