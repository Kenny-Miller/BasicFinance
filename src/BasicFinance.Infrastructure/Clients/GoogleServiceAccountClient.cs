using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;

namespace BasicFinance.Infrastructure.Clients
{
    /// <summary>
    /// The <see cref="GoogleServiceAccountClient"/> is a client that can
    /// perform operations against google api/sdk's using
    /// a service account.
    /// </summary>
    public class GoogleServiceAccountClient
    {
        private readonly string _googleServiceAccountName;
        private readonly DriveService _driveService;
        private readonly SheetsService _sheetsService;

        public GoogleServiceAccountClient(IConfiguration configuration)
        {
            _googleServiceAccountName = configuration["BASICFINANCE-GOOGLE-SERVICEACCOUNT-EMAIL"] ?? throw new ArgumentNullException(nameof(configuration), "Required environmental value BASICFINANCE-GOOGLE-SERVICEACCOUNT-EMAIL was null.");
            var googleServiceAccountPk = configuration["BASICFINANCE-GOOGLE-SERVICEACCOUNT-PK"] ?? throw new ArgumentNullException(nameof(configuration), "Required environmental value BASICFINANCE-GOOGLE-SERVICEACCOUNT-PK was null");
            var decodedPk = Encoding.UTF8.GetString(Convert.FromBase64String(googleServiceAccountPk));

            var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(_googleServiceAccountName)
                .FromPrivateKey(decodedPk));

            var baseInitializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Basic Finance"
            };

            _driveService = new(baseInitializer);
            _sheetsService = new(baseInitializer);
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

        /// <summary>
        /// Removes the service account's access to the specified spreadsheet.
        /// </summary>
        /// <param name="spreadsheetId"></param>
        /// <returns></returns>
        public async Task RevokeSpreadsheetAccessAsync(string spreadsheetId)
        {
            var permissionsRequest = _driveService.Permissions.List(spreadsheetId);
            permissionsRequest.Fields = "permissions(id, emailAddress)";

            var premissionStreamer = new PageStreamer<Permission, PermissionsResource.ListRequest, PermissionList, string>(
                (req, token) => req.PageToken = token,
                res => res.NextPageToken,
                res => res.Permissions);

            Permission? permissionToDelete = null;
            foreach (var permission in premissionStreamer.Fetch(permissionsRequest))
            {
                if (permission.EmailAddress == _googleServiceAccountName)
                {
                    permissionToDelete = permission;
                    break;
                }
            }

            if (permissionToDelete == null)
            {
                return;
            }

            var deleteRequest = _driveService.Permissions.Delete(spreadsheetId, permissionToDelete.Id);
            deleteRequest.SupportsAllDrives = true;
            await deleteRequest.ExecuteAsync();
        }
    }
}
