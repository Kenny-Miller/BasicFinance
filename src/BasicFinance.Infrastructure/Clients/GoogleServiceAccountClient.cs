using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace BasicFinance.Infrastructure.Clients
{
    /// <summary>
    /// The <see cref="GoogleServiceAccountClient"/> is a client that can
    /// perform operations against google api/sdk's using a service account.
    /// </summary>
    public class GoogleServiceAccountClient
    {
        private readonly SheetsService _sheetsService;

        public GoogleServiceAccountClient(ServiceAccountCredential googleServiceAccountCredential)
        {
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = googleServiceAccountCredential,
                ApplicationName = "BasicFinanace"
            };

            _sheetsService = new(initializer);
        }

        /// <summary>
        /// Retrieves a Google Spreadsheet by its Id if it
        /// exists.
        /// </summary>
        /// <param name="googleSheetId"></param>
        /// <returns></returns>
        public async Task<BatchGetValuesResponse?> GetSubSpreadsheetsAsync(string googleSheetId, IReadOnlyList<string> subsheetnames, CancellationToken cancellationToken = default)
        {
            var request = _sheetsService.Spreadsheets.Values.BatchGet(googleSheetId);
            request.Ranges = new(subsheetnames);
            var response = await request.ExecuteAsync(cancellationToken);
            return response;
        }
    }
}
