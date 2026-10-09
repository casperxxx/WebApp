using WebApp.Domain.Entities;

namespace WebApp.Application.Repositories;

/// <summary>
/// Репозиторий для работы с бронированиями в БД
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Найти бронь по Id
    /// </summary>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Id всех броней в статусе Pending (для фонового сервиса)
    /// </summary>
    Task<List<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить бронь и сохранить (вместе с изменениями события в том же контексте)
    /// </summary>
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить изменения (Confirm / Reject и т.п.)
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
