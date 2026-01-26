using System.Security.Claims;
using BasicFinance.Domain.Models;

namespace BasicFinance.Api.Extensions
{
    public static class HttpContextExtensions
    {
        public static UserAuth MapToUser(this HttpContext context)
        {
            var user = context.User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ArgumentNullException(nameof(ClaimTypes.NameIdentifier));

            return new UserAuth(
                userId,
                user.FindFirstValue(ClaimTypes.Name),
                user.FindFirstValue(ClaimTypes.GivenName),
                user.FindFirstValue(ClaimTypes.Surname),
                user.FindFirstValue(ClaimTypes.Email));
        }

    }
}
