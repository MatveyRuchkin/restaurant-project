using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Helpers;

/// <summary>
/// Базовый контроллер с общими методами для работы с текущим пользователем
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Получает ID текущего пользователя из JWT токена
    /// </summary>
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    /// <summary>
    /// Получает имя текущего пользователя из JWT токена
    /// </summary>
    protected string GetCurrentUsername()
    {
        return User?.Identity?.Name ?? SystemUser.Default;
    }

    /// <summary>
    /// Получает роль текущего пользователя из JWT токена
    /// </summary>
    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Проверяет, является ли текущий пользователь администратором
    /// </summary>
    protected bool IsAdmin()
    {
        return GetCurrentUserRole() == Roles.Admin;
    }

    /// <summary>
    /// Проверяет, является ли текущий пользователь официантом или администратором
    /// </summary>
    protected bool IsWaiter()
    {
        var role = GetCurrentUserRole();
        return role == Roles.Waiter || role == Roles.Admin;
    }
}

