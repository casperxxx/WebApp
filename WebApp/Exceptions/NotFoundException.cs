namespace WebApp.Exceptions;

/// <summary>
/// Исключение когда объект не найден
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
