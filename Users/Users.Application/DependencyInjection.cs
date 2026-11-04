using Microsoft.Extensions.DependencyInjection;

namespace Users.Application;

/// <summary>
/// Регистрация сервисов слоя Application (Users)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
