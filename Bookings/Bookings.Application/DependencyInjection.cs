using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Application;

/// <summary>
/// Регистрация сервисов слоя Application (Bookings)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
