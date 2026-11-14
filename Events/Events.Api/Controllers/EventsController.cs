using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Events.Application.Models;
using Events.Application.Services;

namespace Events.Api.Controllers;

/// <summary>
/// HTTP-эндпоинты для событий
/// </summary>
[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    /// <summary>
    /// Создаёт контроллер событий
    /// </summary>
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResultDTO<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResultDTO<EventResponse>>> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _eventService.GetEventsAsync(title, from, to, page, pageSize));
    }

    /// <summary>
    /// Топ-10 популярных событий (по доле проданных мест)
    /// </summary>
    /// <response code="200">Список успешно получен</response>
    [HttpGet("top")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetTop()
    {
        return Ok(await _eventService.GetTopEventsAsync());
    }

    /// <summary>
    /// Получить событие по Id
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <returns>Найденное событие</returns>
    /// <response code="200">Событие успешно найдено</response>
    /// <response code="404">Событие не найдено</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(Guid id)
    {
        return Ok(await _eventService.GetEventAsync(id));
    }

    /// <summary>
    /// Создать новое событие
    /// </summary>
    /// <param name="request">Данные для создания события</param>
    /// <returns>Созданное событие</returns>
    /// <response code="201">Событие успешно создано</response>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="401">Нет токена</response>
    /// <response code="403">Недостаточно прав (нужна роль Admin)</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// <response code="401">Нет токена</response>
    /// <response code="403">Недостаточно прав (нужна роль Admin)</response>
    /// <response code="404">Событие не найдено</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(Guid id, EventDTO request)
    {
        var updated = await _eventService.UpdateEventAsync(id, request);
        return Ok(updated);
    }

    /// <summary>
    /// Удалить событие по Id
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <response code="204">Событие успешно удалено</response>
    /// <response code="401">Нет токена</response>
    /// <response code="403">Недостаточно прав (нужна роль Admin)</response>
    /// <response code="404">Событие не найдено</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteEventAsync(id);
        return NoContent();
    }
}
