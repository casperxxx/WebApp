namespace WebApp.Application.Repositories;

/// <summary>
/// Единица работы: явное сохранение изменений
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Сохранить все изменения в текущем scope
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
