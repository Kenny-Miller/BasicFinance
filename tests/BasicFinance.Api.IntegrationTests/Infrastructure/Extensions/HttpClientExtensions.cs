using System.Net.Http.Json;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;

public static class HttpClientExtensions
{
    /// <summary>
    /// Send a GET request to the specified Uri with a cancellation token as an asynchronous
    ///  operation and attempts to deserialize the response into a <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="client"></param>
    /// <param name="requestUri"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<T> GetResultAsync<T>(this HttpClient client, string requestUri, CancellationToken ct = default)
    {
        var response = await client.GetAsync(requestUri, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new InvalidOperationException("Response content was null.");
    }
}
