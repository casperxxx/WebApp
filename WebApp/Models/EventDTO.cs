using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

/// <summary>
/// DTO модели событий
/// </summary>
public class EventDTO : IValidatableObject
{
    /// <summary>
    /// Название события
    /// </summary>
    [Required(ErrorMessage = "Требуется название события")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала события
    /// </summary>
    [Required(ErrorMessage = "Требуется дата и время начала события")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события
    /// </summary>
    [Required(ErrorMessage = "Требуется дата и время заверешения события")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Сколько всего мест на событии
    /// </summary>
    [Required(ErrorMessage = "Требуется указать количество мест")]
    public int? TotalSeats { get; set; }

    /// <summary>
    /// Проверка корректности укзания дат и времени
    /// </summary>
    /// <param name="validationContext">Валидация</param>
    /// <returns>Ошибки валидации если даты указаны некорректно</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "Дата и время завершения события должно быть позже времени начала.",
                [nameof(EndAt), nameof(StartAt)]);
        }

        if (TotalSeats is null || TotalSeats <= 0)
        {
            yield return new ValidationResult(
                "TotalSeats должно быть больше нуля",
                [nameof(TotalSeats)]);
        }
    }
}
