using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Events.Application.Caching;
using Events.Application.Messaging;
using Events.Application.Repositories;
using Events.Infrastructure.Caching;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Messaging;
using Events.Infrastructure.Repositories;
using StackExchange.Redis;

namespace Events.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет DbContext, репозитории, Redis-кеш, Kafka consumer/publisher
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedBookingRepository, ProcessedBookingRepository>();
        services.AddScoped<IProcessedCancellationRepository, ProcessedCancellationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Redis: ConnectionMultiplexer — синглтон (дорогие соединения)
        var redisSection = configuration.GetSection(RedisCacheOptions.SectionName);
        services.Configure<RedisCacheOptions>(redisSection);
        var redisOptions = new RedisCacheOptions();
        redisSection.Bind(redisOptions);
        services.AddSingleton(redisOptions);
        var redisConnection = redisOptions.ConnectionString;
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect($"{redisConnection},abortConnect=false"));
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton<IBookingEventPublisher, KafkaBookingEventPublisher>();
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingEventsConsumer>();

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
