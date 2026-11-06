using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookings.Api.Extensions;
using Bookings.Application.Models;
using Bookings.Application.Services;

namespace Bookings.Api.Controllers;

/// <summary>
/// Создание брони на событие (EventId приходит в URL)
/// </summary>
[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    /// <summary>
    /// Создаёт контроллер бронирования событий
    /// </summary>
    public EventsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Создать бронь на событие
    /// </summary>
    /// <param name="id">Id события</param>
    /// <response code="202">Бронь создана, обработка ещё идёт</response>
    /// <response code="401">Нет токена</response>
    /// <response code="409">Превышен лимит броней</response>
    [HttpPost("{id}/book")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Book(Guid id)
    {
        var userId = User.GetUserId();
        var booking = await _bookingService.CreateBookingAsync(id, userId);
        return Accepted($"/bookings/{booking.Id}", booking);
    }
}
