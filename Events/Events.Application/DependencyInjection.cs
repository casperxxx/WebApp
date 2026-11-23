using Microsoft.Extensions.DependencyInjection;
using Events.Application.Caching;
using Events.Application.Services;

namespace Events.Application;

/// <summary>
/// Регистрация сервисов слоя Application в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет EventService и синхронизацию кеша
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IEventCacheGate, EventCacheGate>();
        services.AddSingleton<IEventCacheInvalidator, EventCacheInvalidator>();
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
