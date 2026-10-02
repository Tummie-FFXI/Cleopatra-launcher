using System.Net.Http;
using System.Security.Cryptography;
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
        JsonSerializer.Deserialize<UpdateManifest>(
            manifestJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

    if (manifest == null)
    {
        Console.WriteLine("Unable to read update manifest.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Manifest downloaded successfully.");
    Console.WriteLine($"Manifest version: {manifest.Version}");
    Console.WriteLine($"Files in manifest: {manifest.Files.Count}");
    Console.WriteLine();

    foreach (UpdateFile updateFile in manifest.Files)
    {
        Console.Write($"Checking {updateFile.Path}... ");

        if (!File.Exists(updateFile.Path))
        {
            Console.WriteLine("MISSING");
            continue;
        }

        string localHash = CalculateSha256(updateFile.Path);

        if (localHash.Equals(
            updateFile.Sha256,
            StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("CURRENT");
        }
        else
        {
            Console.WriteLine("OUTDATED");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("Unable to check for updates.");
    Console.WriteLine(ex.Message);
}

static string CalculateSha256(string filePath)
{
    using SHA256 sha256 = SHA256.Create();
    using FileStream stream = File.OpenRead(filePath);

    byte[] hash = sha256.ComputeHash(stream);

    return Convert.ToHexString(hash).ToLowerInvariant();
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