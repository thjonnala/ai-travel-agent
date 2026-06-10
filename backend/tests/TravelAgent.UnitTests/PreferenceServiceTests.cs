using TravelAgent.Application.Preferences;
using TravelAgent.Domain;
using TravelAgent.Domain.Entities;

namespace TravelAgent.UnitTests;

public class PreferenceServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly PreferenceService _service;
    private readonly Guid _userId;

    public PreferenceServiceTests()
    {
        var user = new User { ExternalAuthId = "test", Email = "t@example.com", DisplayName = "Test" };
        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();
        _userId = user.Id;

        _service = new PreferenceService(_db.Context);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Get_returns_defaults_when_nothing_saved()
    {
        var dto = await _service.GetAsync(_userId);

        Assert.Equal(BudgetBand.MidRange, dto.BudgetBand);
        Assert.Equal(TripPace.Moderate, dto.Pace);
        Assert.Empty(dto.Interests);
        Assert.Null(dto.UpdatedAt);
    }

    [Fact]
    public async Task Update_upserts_and_roundtrips()
    {
        await _service.UpdateAsync(_userId, new UpdatePreferenceRequest
        {
            BudgetBand = BudgetBand.Budget,
            Pace = TripPace.Packed,
            Interests = ["food", " history ", "food"], // dupes/whitespace cleaned
            DietaryNeeds = "vegetarian",
        });

        var dto = await _service.GetAsync(_userId);

        Assert.Equal(BudgetBand.Budget, dto.BudgetBand);
        Assert.Equal(TripPace.Packed, dto.Pace);
        Assert.Equal(["food", "history"], dto.Interests);
        Assert.Equal("vegetarian", dto.DietaryNeeds);
        Assert.NotNull(dto.UpdatedAt);
        Assert.Single(_db.Context.TravelerPreferences);
    }
}
