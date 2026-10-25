using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Models;
using WebApp.Application.Services;
using WebApp.Extensions;

namespace WebApp.Controllers;

/// <summary>
/// Контроллер для работы с бронированиями
/// </summary>
[ApiController]
[Route("bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    /// <summary>
    /// Создаёт контроллер бронирований
    /// </summary>
    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Получить бронь по Id
    /// </summary>
    /// <param name="id">Id брони</param>
    /// <response code="200">Бронь найдена</response>
    /// <response code="401">Нет токена</response>
    /// <response code="404">Бронь не найдена</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id)
    {
        return Ok(await _bookingService.GetBookingByIdAsync(id));
    }

    /// <summary>
    /// Отменить бронь (свою или любую — для Admin)
    /// </summary>
    /// <param name="id">Id брони</param>
    /// <response code="204">Бронь отменена</response>
    /// <response code="401">Нет токена</response>
    /// <response code="403">Нет прав на отмену чужой брони</response>
    /// <response code="404">Бронь не найдена</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _bookingService.CancelBookingAsync(id, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}
