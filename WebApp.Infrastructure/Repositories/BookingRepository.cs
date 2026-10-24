using Microsoft.EntityFrameworkCore;
using WebApp.Application.Repositories;
using WebApp.Domain.Entities;
using WebApp.Infrastructure.DataAccess;

namespace WebApp.Infrastructure.Repositories;

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

    public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _context.Bookings.Add(booking);
        return Task.CompletedTask;
    }

    public async Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings.CountAsync(
            b => b.UserId == userId
                 && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed),
            cancellationToken);
    }
}
