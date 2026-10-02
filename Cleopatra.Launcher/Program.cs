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
    // Download the update manifest from GitHub.
    string manifestJson =
        await client.GetStringAsync(manifestUrl);

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

    // Check every file listed in the manifest.
    foreach (UpdateFile updateFile in manifest.Files)
    {
        Console.Write($"Checking {updateFile.Path}... ");

        bool needsUpdate = false;

        // File doesn't exist.
        if (!File.Exists(updateFile.Path))
        {
            Console.WriteLine("MISSING");
            needsUpdate = true;
        }
        else
        {
            // File exists. Calculate its SHA-256 hash.
            string localHash =
                CalculateSha256(updateFile.Path);

            // Compare the local hash against the manifest.
            if (localHash.Equals(
                updateFile.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("CURRENT");
            }
            else
            {
                Console.WriteLine("OUTDATED");
                needsUpdate = true;
            }
        }

        // Download missing or outdated files.
        if (needsUpdate)
        {
            Console.WriteLine(
                $"Downloading {updateFile.Path}...");

            bool success =
                await DownloadAndVerifyFile(
                    client,
                    updateFile);

            if (success)
            {
                Console.WriteLine(
                    $"Updated {updateFile.Path} successfully.");
            }
            else
            {
                Console.WriteLine(
                    $"ERROR: {updateFile.Path} failed SHA-256 verification.");

                Console.WriteLine(
                    "The existing file was not changed.");
            }
        }

        Console.WriteLine();
    }

    Console.WriteLine("Update check complete.");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("Unable to check for updates.");
    Console.WriteLine(ex.Message);
}


// ----------------------------------------------------
// DOWNLOAD AND VERIFY FILE
// ----------------------------------------------------

static async Task<bool> DownloadAndVerifyFile(
    HttpClient client,
    UpdateFile updateFile)
{
    string destinationPath = updateFile.Path;

    // Download to a temporary file first.
    string tempPath =
        destinationPath + ".download";

    string? directory =
        Path.GetDirectoryName(destinationPath);

    // Create destination directories if necessary.
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    try
    {
        // Remove an abandoned temporary download
        // from a previous interrupted update.
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        // Download file from GitHub.
        byte[] fileData =
            await client.GetByteArrayAsync(
                updateFile.Url);

        // Write ONLY to the temporary file.
        await File.WriteAllBytesAsync(
            tempPath,
            fileData);

        // Calculate SHA-256 of downloaded file.
        string downloadedHash =
            CalculateSha256(tempPath);

        // Verify the download before replacing anything.
        if (!downloadedHash.Equals(
            updateFile.Sha256,
            StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempPath);

            return false;
        }

        // Verification succeeded.
        // Replace the existing file only now.
        File.Move(
            tempPath,
            destinationPath,
            true);

        return true;
    }
    catch
    {
        // Clean up temporary file if something fails.
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        throw;
    }
}


// ----------------------------------------------------
// SHA-256 HASH CALCULATION
// ----------------------------------------------------

static string CalculateSha256(
    string filePath)
{
    using SHA256 sha256 =
        SHA256.Create();

    using FileStream stream =
        File.OpenRead(filePath);

    byte[] hash =
        sha256.ComputeHash(stream);

    return Convert
        .ToHexString(hash)
        .ToLowerInvariant();
}


// ----------------------------------------------------
// MANIFEST MODELS
// ----------------------------------------------------

public class UpdateManifest
{
    public string Version { get; set; } = "";

    public List<UpdateFile> Files { get; set; } =
        new();
}


public class UpdateFile
{
    public string Path { get; set; } = "";

    public string Url { get; set; } = "";

    public string Sha256 { get; set; } = "";
}