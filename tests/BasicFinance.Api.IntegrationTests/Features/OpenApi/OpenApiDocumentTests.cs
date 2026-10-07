using System.Text.Json;
using System.Text.Json.Nodes;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Features.OpenApi;

public sealed class OpenApiDocumentTests : IClassFixture<ApiClassFixture>
{
    private const string OpenApiDocumentEndpoint = "/openapi/v1.json";

    private const string CommittedDocumentFileName = "openapi.json";

    private const string ActualDocumentFileName = "openapi.actual.json";

    private static readonly JsonSerializerOptions DocumentFormatOptions = new()
    {
        WriteIndented = true
    };

    private readonly ApiClassFixture _fixture;

    public OpenApiDocumentTests(ApiClassFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OpenApiDocument_LiveDocument_MatchesCommittedContract()
    {
        // Arrange
        var expected = LoadCommittedDocument();

        // Act
        var actual = await FetchLiveDocumentAsync(_fixture.CreateClient());
        WriteActualDocumentIfOutdated(actual, expected);
        var difference = FindDifferencePath(NormalizeDocument(actual), NormalizeDocument(expected), "$");

        // Assert
        Assert.True(expected is not null,
            $"Committed contract '{CommittedDocumentFileName}' not found under 'contracts/'. Recapture the live document (written to '{ActualDocumentFileName}' on mismatch) and commit it.");
        Assert.True(difference is null,
            $"OpenAPI document is out of sync with the committed contract. First difference at '{difference}'. " +
            $"Live document written to 'contracts/{ActualDocumentFileName}'. Recapture the contract, re-run 'npm run gen:api', and commit both in the same change.");
    }

    private static async Task<JsonNode> FetchLiveDocumentAsync(HttpClient client)
    {
        var response = await client.GetAsync(OpenApiDocumentEndpoint);
        response.EnsureSuccessStatusCode();

        return JsonNode.Parse(await response.Content.ReadAsStringAsync())
            ?? throw new InvalidOperationException("OpenAPI document response was empty.");
    }

    private static JsonNode? LoadCommittedDocument()
    {
        var documentPath = Path.Combine(ContractsDirectory, CommittedDocumentFileName);
        return File.Exists(documentPath) ? JsonNode.Parse(File.ReadAllText(documentPath)) : null;
    }

    private static void WriteActualDocumentIfOutdated(JsonNode actual, JsonNode? expected)
    {
        if (expected is null || !JsonNode.DeepEquals(NormalizeDocument(actual), NormalizeDocument(expected)))
        {
            var contractsDirectory = ContractsDirectory;
            Directory.CreateDirectory(contractsDirectory);

            var documentPath = Path.Combine(contractsDirectory, ActualDocumentFileName);
            File.WriteAllText(documentPath, actual.ToJsonString(DocumentFormatOptions));
        }
    }

    /// <summary>
    /// Removes the <c>servers</c> entry so the captured document is not tied to the
    /// environment it was captured from.
    /// </summary>
    private static JsonNode? NormalizeDocument(JsonNode? document)
    {
        if (document is JsonObject root)
        {
            root.Remove("servers");
        }

        return document;
    }

    /// <summary>
    /// Reports the shallowest JSON path where the two documents differ.
    /// </summary>
    private static string? FindDifferencePath(JsonNode? actual, JsonNode? expected, string path)
    {
        if (expected is null && actual is null)
        {
            return null;
        }

        if (expected is null)
        {
            return $"{path} (unexpected value: {Describe(actual)})";
        }

        if (actual is null)
        {
            return $"{path} (missing, expected: {Describe(expected)})";
        }

        if (expected is JsonObject expectedObject && actual is JsonObject actualObject)
        {
            foreach (var (key, expectedChild) in expectedObject)
            {
                if (!actualObject.TryGetPropertyValue(key, out var actualChild))
                {
                    return $"{path}.{key} (missing, expected: {Describe(expectedChild)})";
                }

                if (FindDifferencePath(actualChild, expectedChild, $"{path}.{key}") is { } difference)
                {
                    return difference;
                }
            }

            foreach (var (key, actualChild) in actualObject)
            {
                if (!expectedObject.TryGetPropertyValue(key, out _))
                {
                    return $"{path}.{key} (unexpected property: {Describe(actualChild)})";
                }
            }

            return null;
        }

        if (expected is JsonArray expectedArray && actual is JsonArray actualArray)
        {
            var length = Math.Max(actualArray.Count, expectedArray.Count);
            for (var index = 0; index < length; index++)
            {
                var actualChild = index < actualArray.Count ? actualArray[index] : null;
                var expectedChild = index < expectedArray.Count ? expectedArray[index] : null;

                if (FindDifferencePath(actualChild, expectedChild, $"{path}[{index}]") is { } difference)
                {
                    return difference;
                }
            }

            return null;
        }

        return JsonNode.DeepEquals(actual, expected)
            ? null
            : $"{path} (actual: {Describe(actual)}, expected: {Describe(expected)})";
    }

    /// <summary>
    /// Formats a node compactly for inclusion in a failure message.
    /// </summary>
    private static string Describe(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        return node.ToJsonString();
    }

    /// <summary>
    /// Resolves the repository 'contracts/' directory regardless of build configuration or
    /// working directory.
    /// </summary>
    private static string ContractsDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null &&
                   !File.Exists(Path.Combine(directory.FullName, "BasicFinance.slnx")))
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                throw new DirectoryNotFoundException("Repository root marker 'BasicFinance.slnx' was not found.");
            }

            return Path.Combine(directory.FullName, "contracts");
        }
    }
}
