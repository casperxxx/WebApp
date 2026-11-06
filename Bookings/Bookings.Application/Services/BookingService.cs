using Bookings.Application.Messaging;
using Bookings.Application.Models;
using Bookings.Application.Repositories;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using Shared.Contracts;

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
    private readonly IBookingCancelledPublisher _cancelledPublisher;

    public BookingService(
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        IBookingCancelledPublisher cancelledPublisher)
    {
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _cancelledPublisher = cancelledPublisher;
    }

    /// <summary>
    /// Создаёт бронь в статусе Pending; места уменьшает Events по Kafka
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
    /// Отменяет бронь: владелец или администратор.
    /// Если бронь была Confirmed — шлём BookingCancelled, чтобы Events вернул места.
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

            var wasConfirmed = booking.Status == BookingStatus.Confirmed;
            var eventId = booking.EventId;
            var userId = booking.UserId;

            booking.Cancel();
            await _unitOfWork.SaveChangesAsync();

            // Pending ещё не списывал места — Kafka не нужна
            if (wasConfirmed)
            {
                await _cancelledPublisher.PublishAsync(
                    new BookingCancelled(
                        booking.Id,
                        eventId,
                        userId,
                        Seats: 1,
                        DateTime.UtcNow));
            }
        }
        finally
        {
            BookingLock.Release();
        }
    }
}
