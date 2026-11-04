using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Users.Infrastructure;

/// <summary>
/// Регистрация инфраструктуры Users (БД и т.п. — на следующих этапах)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
