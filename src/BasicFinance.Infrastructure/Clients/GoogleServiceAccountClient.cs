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
    public class GoogleServiceAccountClient : IDisposable
    {
        private readonly SheetsService _sheetsService;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the GoogleServiceAccountClient class using the specified Google service
        /// account credentials.
        /// </summary>
        /// <param name="googleServiceAccountCredential">The service account credentials used to authenticate requests to Google APIs.</param>
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
        /// Finalizes an instance of the GoogleServiceAccountClient class and releases unmanaged resources before the
        /// object is reclaimed by garbage collection.
        /// </summary>
        /// <remarks>This destructor calls Dispose to ensure that any unmanaged resources are properly
        /// released. It is invoked automatically by the garbage collector and should not be called directly in
        /// code.</remarks>
        ~GoogleServiceAccountClient()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // Free managed resources
                _sheetsService.Dispose();
            }

            isDisposed = true;
        }
    }
}