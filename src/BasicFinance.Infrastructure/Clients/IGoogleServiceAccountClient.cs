using Google.Apis.Sheets.v4.Data;

namespace BasicFinance.Infrastructure.Clients
{
    public interface IGoogleServiceAccountClient : IDisposable
    {
        /// <summary>
        /// Retrieves a Google Spreadsheet by its Id if it
        /// exists.
        /// </summary>
        /// <param name="googleSheetId"></param>
        /// <param name="subsheetnames"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<BatchGetValuesResponse?> GetSubSpreadsheetsAsync(string googleSheetId, IReadOnlyList<string> subsheetnames, CancellationToken cancellationToken = default);
    }
}
