using Microsoft.Extensions.DependencyInjection;
using TravelAgent.Application.Preferences;
using TravelAgent.Application.Trips;

namespace TravelAgent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITripPlanningService, TripPlanningService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IPreferenceService, PreferenceService>();
        return services;
    }
}
