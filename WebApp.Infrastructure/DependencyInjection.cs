using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Application.Repositories;
using WebApp.Infrastructure.DataAccess;
using WebApp.Infrastructure.Repositories;

namespace WebApp.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет DbContext, PostgreSQL и реализации репозиториев
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}
