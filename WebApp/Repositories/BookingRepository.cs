using Microsoft.EntityFrameworkCore;
using WebApp.DataAccess;
using WebApp.Models;

namespace WebApp.Repositories;

/// <summary>
/// Реализация репозитория бронирований через AppDbContext
/// </summary>
internal sealed class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<List<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _context.Bookings.Add(booking);
        // сохраняет и бронь, и изменение AvailableSeats у отслеживаемого события
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
