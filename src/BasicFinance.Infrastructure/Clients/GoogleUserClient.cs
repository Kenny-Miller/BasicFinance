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
    public class GoogleUserClient
    {
        private readonly ILogger _logger;

        public GoogleUserClient(ILogger<GoogleUserClient> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a Google Spreadsheet by its Id if it
        /// exists.
        /// </summary>
        /// <param name="spreadsheetId"></param>
        /// <returns></returns>
        public async Task<Spreadsheet?> GetSpreadsheetAsync(string spreadsheetId, string accessToken)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Basic Finance"
            });

            var request = sheetsService.Spreadsheets.Get(spreadsheetId);
            var response = await request.ExecuteAsync();
            return response;
        }

        /// <summary>
        /// Updates the specified spreadsheet to permit the Basic Finance
        /// Application to access their spreadsheet.
        /// </summary>
        /// <param name="spreadsheetId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task GrantSpreadSheetAccessAsync(string spreadsheetId, string accessToken)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Basic Finance"
            });

            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "user",
                Role = "reader",
                EmailAddress = "basicfinance-api@basichub.iam.gserviceaccount.com"
            };

            _logger.LogDebug("Granting read access to SpreadSheet: {SpreadheetId}", spreadsheetId);
            var request = driveService.Permissions.Create(permission, spreadsheetId);
            request.SendNotificationEmail = false;
            await request.ExecuteAsync();
        }
    }
}
