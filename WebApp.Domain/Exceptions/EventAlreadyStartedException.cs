namespace WebApp.Domain.Exceptions;

/// <summary>
/// Исключение при попытке забронировать событие, которое уже началось
/// </summary>
public class EventAlreadyStartedException : Exception
{
    public EventAlreadyStartedException(string message) : base(message) { }
}
