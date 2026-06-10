using TravelAgent.Infrastructure.Identity;

namespace TravelAgent.UnitTests;

public class DemoCurrentUserServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Provisions_the_demo_user_on_first_use_and_reuses_it()
    {
        var service = new DemoCurrentUserService(_db.Context);

        var first = await service.GetCurrentUserIdAsync();
        var second = await service.GetCurrentUserIdAsync();

        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, second);
        var user = Assert.Single(_db.Context.Users);
        Assert.Equal(first, user.Id);
        Assert.Equal("Demo Traveler", user.DisplayName);
    }

    [Fact]
    public async Task Separate_instances_resolve_the_same_user()
    {
        var first = await new DemoCurrentUserService(_db.Context).GetCurrentUserIdAsync();
        var second = await new DemoCurrentUserService(_db.Context).GetCurrentUserIdAsync();

        Assert.Equal(first, second);
        Assert.Single(_db.Context.Users);
    }
}
