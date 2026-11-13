using Events.Domain.Entities;

namespace Events.Application.Repositories;

/// <summary>
/// Учёт уже обработанных BookingConfirmed (идемпотентность)
/// </summary>
public interface IProcessedBookingRepository
{
    /// <summary>
    /// Была ли бронь уже обработана
    /// </summary>
    Task<bool> ExistsAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Пометить бронь как обработанную (без SaveChanges)
    /// </summary>
    Task AddAsync(ProcessedBooking processed, CancellationToken cancellationToken = default);
}
