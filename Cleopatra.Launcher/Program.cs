using System.Net.Http;
using System.Text.Json;

Console.WriteLine("Cleopatra Launcher");
Console.WriteLine("------------------");
Console.WriteLine("Checking for updates...");

string manifestUrl =
    "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-launcher/main/manifest.json";

using HttpClient client = new HttpClient();

try
{
    string manifestJson = await client.GetStringAsync(manifestUrl);

    UpdateManifest? manifest =
        JsonSerializer.Deserialize<UpdateManifest>(manifestJson);

    if (manifest == null)
    {
        Console.WriteLine("Unable to read update manifest.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Manifest downloaded successfully.");
    Console.WriteLine($"Manifest version: {manifest.Version}");
    Console.WriteLine($"Files in manifest: {manifest.Files.Count}");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("Unable to check for updates.");
    Console.WriteLine(ex.Message);
}

public class UpdateManifest
{
    public string Version { get; set; } = "";
    public List<UpdateFile> Files { get; set; } = new();
}

public class UpdateFile
{
    public string Path { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
}