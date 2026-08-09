using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

/// <summary>
/// Контроллер для работы с бронированиями
/// </summary>
[ApiController]
[Route("bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Получить бронь по Id
    /// </summary>
    /// <param name="id">Id брони</param>
    /// <response code="200">Бронь найдена</response>
    /// <response code="404">Бронь не найдена</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Booking>> GetById(Guid id)
    {
        return Ok(await _bookingService.GetBookingByIdAsync(id));
    }
}
