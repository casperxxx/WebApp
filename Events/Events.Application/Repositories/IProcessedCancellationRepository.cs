using Events.Domain.Entities;

namespace Events.Application.Repositories;

/// <summary>
/// Учёт уже обработанных BookingCancelled (идемпотентность)
/// </summary>
public interface IProcessedCancellationRepository
{
    /// <summary>
    /// Была ли отмена уже обработана
    /// </summary>
    Task<bool> ExistsAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Пометить отмену как обработанную (без SaveChanges)
    /// </summary>
    Task AddAsync(ProcessedCancellation processed, CancellationToken cancellationToken = default);
}
