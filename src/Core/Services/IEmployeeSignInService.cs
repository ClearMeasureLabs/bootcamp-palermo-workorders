namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Signs the current HTTP request in or out as an employee using server-side authentication.
/// </summary>
public interface IEmployeeSignInService
{
    /// <summary>
    /// Establishes an authenticated session for the given employee username.
    /// </summary>
    Task SignInAsync(string userName);

    /// <summary>
    /// Clears the authenticated employee session for the current request.
    /// </summary>
    Task SignOutAsync();
}
