using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    /// <summary>
    /// Получить список событий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="title">Поиск по названию</param>
    /// <param name="from">События, которые начинаются не раньше указанной даты</param>
    /// <param name="to">События, которые заканчиваются не позже указанной даты</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <response code="200">Список событий успешно получен</response>
    /// <response code="400">Ошибка получения списка</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDTO<Event>), StatusCodes.Status200OK)]
    public ActionResult<PaginatedResultDTO<Event>> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(_eventService.GetEvents(title, from, to, page, pageSize));
    }

    /// <summary>
    /// Получить событие по Id
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <returns>Найденное событие</returns>
    /// <response code="200">Событие успешно найдено</response>
    /// <response code="404">Событие не найдено</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetById(Guid id)
    {
        return Ok(_eventService.GetEvent(id));
    }

    /// <summary>
    /// Создать новое событие
    /// </summary>
    /// <param name="request">Данные для создания события</param>
    /// <returns>Созданное событие</returns>
    /// <response code="201">Событие успешно создано</response>
    /// <response code="400">Некорректные данные запроса</response>
    [HttpPost]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(EventDTO request)
    {
        var eventItem = await _eventService.CreateEventAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = eventItem.Id }, eventItem);
    }

    /// <summary>
    /// Обновить событие по Id
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <param name="request">Новые данные события</param>
    /// <returns>Обновлённое событие</returns>
    /// <response code="200">Событие успешно обновлено</response>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="404">Событие не найдено</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Update(Guid id, EventDTO request)
    {
        var eventItem = Event.FromUpdate(request);

        _eventService.UpdateEvent(id, eventItem);

        return Ok(_eventService.GetEvent(id));
    }

    /// <summary>
    /// Удалить событие по Id
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <response code="204">Событие успешно удалено</response>
    /// <response code="404">Событие не найдено</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        _eventService.DeleteEvent(id);
        return NoContent();
    }

    /// <summary>
    /// Создать бронь на событие
    /// </summary>
    /// <param name="id">Id события</param>
    /// <response code="202">Бронь создана, обработка ещё идёт</response>
    /// <response code="404">Событие не найдено</response>
    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Booking>> Book(Guid id)
    {
        var booking = await _bookingService.CreateBookingAsync(id);
        return Accepted($"/bookings/{booking.Id}", booking);
    }
}
