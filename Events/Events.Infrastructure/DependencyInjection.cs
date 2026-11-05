using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Events.Application.Repositories;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Repositories;

namespace Events.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет DbContext, PostgreSQL и репозитории Events
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Применяет миграции для реляционной БД
    /// </summary>
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
    }
}
