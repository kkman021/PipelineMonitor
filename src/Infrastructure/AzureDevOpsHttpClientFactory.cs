using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;

namespace AzureSummary.Infrastructure;

public class AzureDevOpsHttpClientFactory
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    public HttpClient GetOrCreate(string pat)
    {
        return _clients.GetOrAdd(pat, CreateClient);
    }

    private static HttpClient CreateClient(string pat)
    {
        var client = new HttpClient();
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }
}
