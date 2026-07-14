using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace BasicFinance.Api.Common.Authentication
{
    /// <summary>
    /// The <see cref="AuthenticatedUserMiddleware"/> is middleware that handles
    /// mapping a <see cref="ClaimsPrincipal"/>'s user claims into an <see cref="AuthenticatedUser"/>
    /// record.
    /// </summary>
    public static class AuthenticatedUserMiddleware
    {
        /// <summary>
        /// Gets a predefined <see cref="ProblemDetails"/> instance representing an HTTP 401 Unauthorized error
        /// response.
        /// </summary>
        private static ProblemDetails Unauthorized => new()
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "User is not authenticated.",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
        };

        /// <summary>
        /// Load an <see cref="AuthenticatedUser" /> record with claims
        /// from the authenticated in user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static (ProblemDetails, AuthenticatedUser?) Load(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return (Unauthorized, null);
            }

            var authenticatedUser = new AuthenticatedUser(
              userId,
              user.FindFirstValue(ClaimTypes.Name),
              user.FindFirstValue(ClaimTypes.GivenName),
              user.FindFirstValue(ClaimTypes.Surname),
              user.FindFirstValue(ClaimTypes.Email));

            return (WolverineContinue.NoProblems, authenticatedUser);
        }
    }
}