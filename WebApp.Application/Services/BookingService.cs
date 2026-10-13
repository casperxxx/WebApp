using WebApp.Application.Models;
using WebApp.Application.Repositories;
using WebApp.Domain.Entities;
using WebApp.Domain.Exceptions;

namespace WebApp.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
internal class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Создаёт бронь в статусе Pending
    /// </summary>
    public async Task<BookingResponse> CreateBookingAsync(Guid eventId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(eventId);
            if (eventItem is null)
            {
                throw new NotFoundException($"Событие с id {eventId} не найдено");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException($"Нет свободных мест для события {eventItem.Title} c id:{eventId}");
            }

            var booking = Booking.CreatePending(eventId);
            await _bookingRepository.AddAsync(booking);
            // одно сохранение: и бронь, и уменьшенные AvailableSeats
            await _unitOfWork.SaveChangesAsync();

            return BookingResponse.From(booking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <summary>
    /// Возвращает бронь по Id
    /// </summary>
    public async Task<BookingResponse> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null)
        {
            throw new NotFoundException($"Бронь с id {bookingId} не найдена");
        }

        return BookingResponse.From(booking);
    }
}
