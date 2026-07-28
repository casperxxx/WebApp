namespace WebApp.Models;

/// <summary>
/// Результат с пагинацией
/// </summary>
public class PaginatedResultDTO<T>
{
    /// <summary>
    /// Общее количество записей
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Список событий на текущей странице
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    public int PageSize { get; set; }
}
