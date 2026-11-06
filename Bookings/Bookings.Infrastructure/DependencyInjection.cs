using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bookings.Application.Repositories;
using Bookings.Infrastructure.DataAccess;
using Bookings.Infrastructure.Repositories;

namespace Bookings.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет DbContext, PostgreSQL и репозитории Bookings
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Применяет миграции для реляционной БД
    /// </summary>
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
    }
}
