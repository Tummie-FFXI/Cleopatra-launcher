using Cleopatra.Launcher.Services;

Console.WriteLine("Cleopatra Launcher");
Console.WriteLine("------------------");
Console.WriteLine("Checking for updates...");

const string manifestUrl =
    "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-launcher/main/manifest.json";

using HttpClient client = new HttpClient();

ManifestService manifestService =
    new ManifestService(client);

FileUpdater fileUpdater =
    new FileUpdater(client);

try
{
    var manifest =
        await manifestService.GetManifestAsync(manifestUrl);

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

    foreach (var updateFile in manifest.Files)
    {
        string status =
            fileUpdater.GetFileStatus(updateFile);

        Console.WriteLine(
            $"Checking {updateFile.Path}... {status}");

        if (status == "CURRENT")
        {
            Console.WriteLine();
            continue;
        }

        Console.WriteLine(
            $"Downloading {updateFile.Path}...");

        bool success =
            await fileUpdater.DownloadAndVerifyAsync(
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