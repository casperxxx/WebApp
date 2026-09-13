using WebApp.Models;

namespace WebApp.Repositories;

/// <summary>
/// Репозиторий для работы с событиями в БД
/// </summary>
internal interface IEventRepository
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
    /// Добавить событие и сохранить
    /// </summary>
    Task AddAsync(Event eventItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить изменения по уже загруженному событию
    /// </summary>
    Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить событие и сохранить
    /// </summary>
    Task DeleteAsync(Event eventItem, CancellationToken cancellationToken = default);
}
