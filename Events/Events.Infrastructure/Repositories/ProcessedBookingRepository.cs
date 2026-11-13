using Microsoft.EntityFrameworkCore;
using Events.Application.Repositories;
using Events.Domain.Entities;
using Events.Infrastructure.DataAccess;

namespace Events.Infrastructure.Repositories;

/// <summary>
/// Репозиторий обработанных броней через EventsDbContext
/// </summary>
internal sealed class ProcessedBookingRepository : IProcessedBookingRepository
{
    private readonly EventsDbContext _context;

    public ProcessedBookingRepository(EventsDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return _context.ProcessedBookings.AnyAsync(p => p.BookingId == bookingId, cancellationToken);
    }

    public Task AddAsync(ProcessedBooking processed, CancellationToken cancellationToken = default)
    {
        _context.ProcessedBookings.Add(processed);
        return Task.CompletedTask;
    }
}
