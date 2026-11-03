namespace WebApp.Domain.Exceptions;

/// <summary>
/// Исключение при превышении лимита активных броней пользователя
/// </summary>
public class BookingLimitExceededException : Exception
{
    public BookingLimitExceededException(string message) : base(message) { }
}
