using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BasicFinance.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Google Service Account Credential to the service collection.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException "></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static IServiceCollection AddGoogleServiceAccountCredentials(this IServiceCollection services)
        {
            return services.AddSingleton(static serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();

                var credentialConfigLocation = configuration["GOOGLE-APPLICATION-CREDENTIALS"];
                if (string.IsNullOrEmpty(credentialConfigLocation))
                {
                    throw new InvalidOperationException("Required environment variable 'GOOGLE_APPLICATION_CREDENTIALS' is not set or is empty.");
                }

                if (!File.Exists(credentialConfigLocation))
                {
                    throw new FileNotFoundException("Google service account credential file not found.", credentialConfigLocation);
                }

                using var stream = File.OpenRead(credentialConfigLocation);
                var credentials = ServiceAccountCredential.FromServiceAccountData(stream);
                credentials.Scopes =
                [
                    DriveService.Scope.Drive,
                ];

                return credentials;
            });
        }
    }
}
