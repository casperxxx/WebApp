using Microsoft.Extensions.DependencyInjection;
using Bookings.Application.Services;

namespace Bookings.Application;

/// <summary>
/// Регистрация сервисов слоя Application в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет BookingService, фон Confirm и публикацию outbox
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();
        services.AddHostedService<OutboxPublisherService>();
        return services;
    }
}
