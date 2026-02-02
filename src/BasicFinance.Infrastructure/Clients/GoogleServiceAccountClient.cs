using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace BasicFinance.Infrastructure.Clients
{
    /// <summary>
    /// The <see cref="GoogleServiceAccountClient"/> is a client that can
    /// perform operations against google api/sdk's using
    /// a service account.
    /// </summary>
    public class GoogleServiceAccountClient
    {
        private readonly ServiceAccountCredential _googleServiceAccountCredential;
        private readonly SheetsService _sheetsService;

        public GoogleServiceAccountClient(ServiceAccountCredential googleServiceAccountCredential)
        {
            _googleServiceAccountCredential = googleServiceAccountCredential;
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = _googleServiceAccountCredential,
                ApplicationName = "BasicFinanace"
            };

            _sheetsService = new(initializer);
        }

        /// <summary>
        /// Retrieves a Google Spreadsheet by its Id if it
        /// exists.
        /// </summary>
        /// <param name="spreadsheetId"></param>
        /// <returns></returns>
        public async Task<Spreadsheet?> GetSpreadsheetAsync(string spreadsheetId)
        {
            var request = _sheetsService.Spreadsheets.Get(spreadsheetId);
            var response = await request.ExecuteAsync();
            return response;
        }
    }
}
