using Bookings.Application.Models;
using Bookings.Application.Repositories;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;

namespace Bookings.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
internal class BookingService : IBookingService
{
    /// <summary>
    /// Максимум активных броней (Pending + Confirmed) на одного пользователя
    /// </summary>
    public const int MaxActiveBookings = 10;

    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Создаёт бронь в статусе Pending; места уменьшает Events по Kafka (этап 6–7)
    /// </summary>
    public async Task<BookingResponse> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var activeCount = await _bookingRepository.CountActiveByUserAsync(userId);
            if (activeCount >= MaxActiveBookings)
            {
                throw new BookingLimitExceededException(
                    $"Превышен лимит активных броней ({MaxActiveBookings})");
            }

            var booking = Booking.CreatePending(eventId, userId);
            await _bookingRepository.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return BookingResponse.From(booking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <summary>
    /// Возвращает бронь по Id; чужую бронь может смотреть только Admin
    /// </summary>
    public async Task<BookingResponse> GetBookingByIdAsync(Guid bookingId, Guid currentUserId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null)
        {
            throw new NotFoundException($"Бронь с id {bookingId} не найдена");
        }

        if (booking.UserId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Недостаточно прав для просмотра этой брони");
        }

        return BookingResponse.From(booking);
    }

    /// <summary>
    /// Отменяет бронь: владелец или администратор
    /// </summary>
    public async Task CancelBookingAsync(Guid bookingId, Guid currentUserId, bool isAdmin)
    {
        await BookingLock.WaitAsync();
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking is null)
            {
                throw new NotFoundException($"Бронь с id {bookingId} не найдена");
            }

            if (booking.UserId != currentUserId && !isAdmin)
            {
                throw new ForbiddenException("Недостаточно прав для отмены этой брони");
            }

            booking.Cancel();
            await _unitOfWork.SaveChangesAsync();
        }
        finally
        {
            BookingLock.Release();
        }
    }
}
