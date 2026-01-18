using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace BasicFinance.Infrastructure.Clients
{
    public class GoogleSpreadsheetClient
    {
        private readonly SheetsService _sheetsService;
        public GoogleSpreadsheetClient(SheetsService sheetsService) { 
            _sheetsService = sheetsService;
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
