using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;

namespace BasicFinance.Infrastructure.Clients
{
    /// <summary>
    /// The <see cref="GoogleUserClient"/> is a client that can
    /// perform operations against google api/sdk's using
    /// a pre-authenticated user.
    /// </summary>
    public partial class GoogleUserClient
    {
        private readonly ServiceAccountCredential _googleServiceAccountCredential;
        private readonly ILogger _logger;

        public GoogleUserClient(ServiceAccountCredential googleServiceAccountCredential, ILogger<GoogleUserClient> logger)
        {
            _googleServiceAccountCredential = googleServiceAccountCredential;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a Google Spreadsheet by its Id if it
        /// exists.
        /// </summary>
        /// <param name="googleSpreadsheetId"></param>
        /// <param name="accessToken">The access token for the authenticated user.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns></returns>
        public async Task<Spreadsheet?> GetSpreadsheetAsync(string googleSpreadsheetId, string accessToken, CancellationToken cancellationToken = default)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            using var sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Basic Finance"
            });

            var request = sheetsService.Spreadsheets.Get(googleSpreadsheetId);
            var response = await request.ExecuteAsync(cancellationToken);
            if (response == null)
            {
                LogSpreadsheetNotFound(_logger, googleSpreadsheetId);
            }

            return response;
        }

        /// <summary>
        /// Updates the specified spreadsheet to permit the Basic Finance
        /// Application to access their spreadsheet.
        /// </summary>
        /// <param name="googleSpreadsheetId"></param>
        /// <param name="accessToken"></param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns></returns>
        public async Task GrantSpreadSheetAccessAsync(string googleSpreadsheetId, string accessToken, CancellationToken cancellationToken = default)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            using var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Basic Finance"
            });

            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "user",
                Role = "reader",
                EmailAddress = _googleServiceAccountCredential.Id
            };

            LogGrantingSpreadsheetAccess(_logger, googleSpreadsheetId);
            var request = driveService.Permissions.Create(permission, googleSpreadsheetId);
            request.SendNotificationEmail = false;
            await request.ExecuteAsync(cancellationToken);
        }


        [LoggerMessage(
          EventName = nameof(LogSpreadsheetNotFound),
          Level = LogLevel.Error,
          Message = "Unable to retrive Google SpreadSheet: {GoogleSpreadsheetId}")]
        private static partial void LogSpreadsheetNotFound(ILogger logger, string googleSpreadsheetId);

        [LoggerMessage(
          EventName = nameof(LogGrantingSpreadsheetAccess),
          Level = LogLevel.Information,
          Message = "Granting read access to Google SpreadSheet: {GoogleSpreadsheetId}")]
        private static partial void LogGrantingSpreadsheetAccess(ILogger logger, string googleSpreadsheetId);
    }
}