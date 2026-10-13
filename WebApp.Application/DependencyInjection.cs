using Microsoft.Extensions.DependencyInjection;
using WebApp.Application.Services;

namespace WebApp.Application;

/// <summary>
/// Регистрация сервисов слоя Application в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет use case-сервисы и фоновую обработку броней
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}
