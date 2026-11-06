using Microsoft.Extensions.DependencyInjection;
using Bookings.Application.Services;

namespace Bookings.Application;

/// <summary>
/// Регистрация сервисов слоя Application в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет BookingService и фоновую обработку броней
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();
        return services;
    }
}
