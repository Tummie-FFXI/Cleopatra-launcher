using Cleopatra.Launcher.Services;

Console.WriteLine("Cleopatra Launcher");
Console.WriteLine("------------------");
Console.WriteLine("Checking for updates...");

const string manifestUrl =
    "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-launcher/main/manifest.json";

// TEMPORARY:
// Safe test Pivot directory.
// Later PivotService will determine the proper location
// based on the player's Windows loader/Pivot setup.
string testPivotRoot =
    Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile),
        "CleopatraPivotTest");

using HttpClient client = new HttpClient();

ManifestService manifestService =
    new ManifestService(client);

FileUpdater fileUpdater =
    new FileUpdater(client);

PivotService pivotService =
    new PivotService(testPivotRoot);

try
{
    // Make sure our Cleopatra Pivot directory exists.
    pivotService.EnsureCleopatraDirectory();

    Console.WriteLine();
    Console.WriteLine(
        $"Cleopatra mod directory: {pivotService.GetCleopatraRoot()}");

    // Download and read the manifest.
    var manifest =
        await manifestService.GetManifestAsync(
            manifestUrl);

    if (manifest == null)
    {
        Console.WriteLine(
            "Unable to read update manifest.");

        return;
    }

    Console.WriteLine();
    Console.WriteLine(
        "Manifest downloaded successfully.");

    Console.WriteLine(
        $"Manifest version: {manifest.Version}");

    Console.WriteLine(
        $"Files in manifest: {manifest.Files.Count}");

    Console.WriteLine();

    // Check every file listed in the manifest.
    foreach (var updateFile in manifest.Files)
    {
        // Convert the manifest's relative path
        // into the real Cleopatra Pivot destination.
        string destinationPath =
            pivotService.GetDestinationPath(
                updateFile.Path);

        string status =
            fileUpdater.GetFileStatus(
                updateFile,
                destinationPath);

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
                updateFile,
                destinationPath);

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

    Console.WriteLine(
        "Update check complete.");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine(
        "Unable to check for updates.");

    Console.WriteLine(ex.Message);
}