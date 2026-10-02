using System.Text.Json;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class ManifestService
{
    private readonly HttpClient _client;

    public ManifestService(HttpClient client)
    {
        _client = client;
    }

    public async Task<UpdateManifest?> GetManifestAsync(string manifestUrl)
    {
        string manifestJson =
            await _client.GetStringAsync(manifestUrl);

        return JsonSerializer.Deserialize<UpdateManifest>(
            manifestJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }
}