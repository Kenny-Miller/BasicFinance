namespace BasicFinance.Api.Common.Authentication
{
    /// <summary>
    /// The <see cref="AuthenticatedUser"/> represents the mapped claims
    /// of an authenticated user.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="FullName"></param>
    /// <param name="FirstName"></param>
    /// <param name="LastName"></param>
    /// <param name="Email"></param>
    public sealed record AuthenticatedUser(string Id, string? FullName, string? FirstName, string? LastName, string? Email);
}
