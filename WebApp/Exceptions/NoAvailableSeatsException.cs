namespace WebApp.Exceptions;

/// <summary>
/// Исключение когда на событии нет свободных мест
/// </summary>
public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message) { }
}
