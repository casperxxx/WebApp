namespace WebApp.Domain.Exceptions;

/// <summary>
/// Исключение при отсутствии прав на операцию
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}