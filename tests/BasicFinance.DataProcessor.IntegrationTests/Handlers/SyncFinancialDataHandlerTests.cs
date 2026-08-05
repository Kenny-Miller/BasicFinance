using BasicFinance.DataProcessor.IntegrationTests.Infrastructure;
using BasicFinance.Domain.Commands;
using Google.Apis.Sheets.v4.Data;
using NSubstitute;
using Wolverine.Tracking;
using Xunit;

namespace BasicFinance.DataProcessor.IntegrationTests.Handlers
{
    public class SyncFinancialDataHandlerTests : DataProcessorTestBase
    {
        public SyncFinancialDataHandlerTests(DataProcessorAppFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Handle_SpreadsheetNotFound_ReturnsWithoutProcessing()
        {
            // Arrange
            var a = TestFixtureGuid;
            MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
                .Returns((BatchGetValuesResponse?)null);

            var command = new SyncFinancialData(Guid.NewGuid());

            // Act
            var result = await Host.InvokeMessageAndWaitAsync(command);

            // Assert
            await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
        }

        [Fact]
        public async Task Handle_SpreadsheetNotFound_ReturnsWithoutProcessing2()
        {
            // Arrange
            var a = TestFixtureGuid;
            MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
                .Returns((BatchGetValuesResponse?)null);

            var command = new SyncFinancialData(Guid.NewGuid());

            // Act
            var result = await Host.InvokeMessageAndWaitAsync(command);

            // Assert
            await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
        }
    }
}
