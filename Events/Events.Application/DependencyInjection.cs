using Microsoft.Extensions.DependencyInjection;
using Events.Application.Services;

namespace Events.Application;

/// <summary>
/// Регистрация сервисов слоя Application в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет EventService
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
