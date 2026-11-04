using Microsoft.Extensions.DependencyInjection;

namespace Events.Application;

/// <summary>
/// Регистрация сервисов слоя Application (Events)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
