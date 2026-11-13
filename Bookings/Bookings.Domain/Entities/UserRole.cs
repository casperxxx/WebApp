namespace Bookings.Domain.Entities;

/// <summary>
/// Роль пользователя в системе (для проверки claims JWT)
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Обычный пользователь: бронирование и отмена своих броней
    /// </summary>
    User,

    /// <summary>
    /// Администратор: управление событиями и отмена любых броней
    /// </summary>
    Admin
}
