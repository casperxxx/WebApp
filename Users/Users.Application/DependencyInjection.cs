using Microsoft.Extensions.DependencyInjection;
using Users.Application.Services;

namespace Users.Application;

/// <summary>
/// Регистрация сервисов слоя Application в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет AuthService
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
