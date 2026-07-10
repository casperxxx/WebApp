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
    [Range(typeof(DateTime), "2026-07-01", "2028-12-31", ErrorMessage = "Некорректная дата начала события")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события
    /// </summary>
    [Required(ErrorMessage = "Требуется дата и время заверешения события")]
    [Range(typeof(DateTime), "2026-07-01", "2028-12-31", ErrorMessage = "Некорректная дата завершения события")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Проверка корректности укзания дат и времени
    /// </summary>
    /// <param name="validationContext">Валидация</param>
    /// <returns>Ошибки валидации если даты указаны некорректно</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt < StartAt)
        {
            yield return new ValidationResult(
                "Дата и время завершения события не может быть раньше времени начала.",
                [nameof(EndAt), nameof(StartAt)]);
        }
    }
}
