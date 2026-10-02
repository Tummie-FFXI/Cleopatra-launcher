using Cleopatra.Launcher.Services;

Console.WriteLine("Cleopatra Launcher");
Console.WriteLine("------------------");

const string manifestUrl =
    "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-launcher/main/manifest.json";

// ----------------------------------------------------
// TEMPORARY DEVELOPMENT SETTING
// ----------------------------------------------------
// For Mac testing we're using our fake Windower install.
// Later the Windows launcher will detect/select the user's
// actual Windower or Ashita installation.

string home =
    Environment.GetFolderPath(
        Environment.SpecialFolder.UserProfile);

string loaderRoot =
    Path.Combine(
        home,
        "CleopatraLoaderTests",
        "Windower");

// ----------------------------------------------------
// SERVICES
// ----------------------------------------------------

using HttpClient client = new HttpClient();

ManifestService manifestService =
    new ManifestService(client);

FileUpdater fileUpdater =
    new FileUpdater(client);

PivotService pivotService =
    new PivotService();

try
{
    // ------------------------------------------------
    // DETECT LOADER / PIVOT
    // ------------------------------------------------

    Console.WriteLine(
        "Detecting loader and Pivot...");

    bool detected =
        pivotService.DetectFromLoaderRoot(
            loaderRoot);

    if (!detected)
    {
        Console.WriteLine();
        Console.WriteLine(
            "No supported Pivot installation was found.");

        return;
    }

    var installation =
        pivotService.Installation!;

    Console.WriteLine(
        $"Loader detected: {installation.LoaderType}");

    Console.WriteLine(
        $"Pivot location: {installation.PivotRoot}");

    // ------------------------------------------------
    // READ PIVOT CONFIGURATION
    // ------------------------------------------------

    var pivotConfiguration =
        pivotService.ReadConfiguration();

    Console.WriteLine(
        $"Pivot overlay root: {pivotConfiguration.OverlayRoot}");

    // ------------------------------------------------
    // ENABLE CLEOPATRA OVERLAY
    // ------------------------------------------------

    if (!pivotConfiguration.CleopatraEnabled)
    {
        Console.WriteLine(
            "Enabling Cleopatra Pivot overlay...");

        pivotService.EnsureCleopatraOverlay();

        Console.WriteLine(
            "Cleopatra Pivot overlay enabled.");
    }
    else
    {
        Console.WriteLine(
            "Cleopatra Pivot overlay already enabled.");
    }

    // ------------------------------------------------
    // CREATE CLEOPATRA DIRECTORY
    // ------------------------------------------------

    pivotService.EnsureCleopatraDirectory();

    Console.WriteLine(
        $"Cleopatra directory: {pivotService.GetCleopatraRoot()}");

    // ------------------------------------------------
    // GET UPDATE MANIFEST
    // ------------------------------------------------

    Console.WriteLine();
    Console.WriteLine(
        "Checking for updates...");

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

    // ------------------------------------------------
    // CHECK AND UPDATE FILES
    // ------------------------------------------------

    foreach (var updateFile in manifest.Files)
    {
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
        "Cleopatra encountered an error.");

    Console.WriteLine(
        ex.Message);
}