namespace WebApp.Domain.Exceptions;

/// <summary>
/// Нарушение доменного/бизнес-правила (отдаём клиенту как 400)
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message) { }
}
