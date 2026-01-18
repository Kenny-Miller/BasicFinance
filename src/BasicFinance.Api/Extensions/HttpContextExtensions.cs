using System.Security.Claims;
using BasicFinance.Domain.Models;

namespace BasicFinance.Api.ParameterStrategy
{
    public static class HttpContextExtensions
    {
        public static User MapToUser(this HttpContext context)
        {
            var user = context.User;
            var temp = user.FindFirst("name");
            if (user.FindFirstValue(ClaimTypes.NameIdentifier) is string id &&
               user.FindFirstValue(ClaimTypes.Name) is string fullName &&
               user.FindFirstValue(ClaimTypes.GivenName) is string firstName &&
               user.FindFirstValue(ClaimTypes.Surname) is string lastName &&
               user.FindFirstValue(ClaimTypes.Email) is string email)
            {
                return new User(id, fullName, firstName, lastName, email);
            }

            throw new ArgumentNullException("Context did not have enough claims to build logged in user");
        }
    }
}
