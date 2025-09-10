using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Extensions for ControllerBase to easily access authenticated user information
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Get the current authenticated user's ID
    /// </summary>
    /// <param name="controller">Controller instance</param>
    /// <returns>User ID if authenticated, null otherwise</returns>
    public static Guid? GetUserId(this ControllerBase controller)
    {
        return controller.HttpContext.Items["UserId"] as Guid?;
    }

    /// <summary>
    /// Get the current authenticated user's email
    /// </summary>
    /// <param name="controller">Controller instance</param>
    /// <returns>User email if authenticated, null otherwise</returns>
    public static string? GetUserEmail(this ControllerBase controller)
    {
        return controller.HttpContext.Items["UserEmail"] as string;
    }

    /// <summary>
    /// Get the current authenticated user's full name
    /// </summary>
    /// <param name="controller">Controller instance</param>
    /// <returns>User full name if authenticated, null otherwise</returns>
    public static string? GetUserFullName(this ControllerBase controller)
    {
        return controller.HttpContext.Items["UserFullName"] as string;
    }

    /// <summary>
    /// Check if the current request is authenticated
    /// </summary>
    /// <param name="controller">Controller instance</param>
    /// <returns>True if authenticated, false otherwise</returns>
    public static bool IsAuthenticated(this ControllerBase controller)
    {
        return controller.GetUserId().HasValue;
    }
}
