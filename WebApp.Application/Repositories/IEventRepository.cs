using WebApp.Domain.Entities;

namespace WebApp.Application.Repositories;

/// <summary>
/// Репозиторий для работы с событиями в БД
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Список событий с фильтрами и пагинацией
    /// </summary>
    Task<(List<Event> Items, int TotalCount)> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти событие по Id
    /// </summary>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить событие в контекст (без SaveChanges)
    /// </summary>
    Task AddAsync(Event eventItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить событие из контекста (без SaveChanges)
    /// </summary>
    void Delete(Event eventItem);
}
