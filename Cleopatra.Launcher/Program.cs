using Cleopatra.Launcher.Services;

Console.WriteLine("Cleopatra Launcher");
Console.WriteLine("------------------");

const string manifestUrl =
    "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-launcher/main/manifest.json";

// ----------------------------------------------------
// SERVICES
// ----------------------------------------------------

using HttpClient client = new HttpClient();

LoaderDetectionService loaderDetectionService =
    new LoaderDetectionService();

ManifestService manifestService =
    new ManifestService(client);

FileUpdater fileUpdater =
    new FileUpdater(client);

PivotService pivotService =
    new PivotService();

InstallationStateService installationStateService =
    new InstallationStateService();

// ----------------------------------------------------
// TEMPORARY DEVELOPMENT PATHS
// ----------------------------------------------------
// These fake paths are only for development on the Mac.
// Later Windows discovery will supply the real candidates.

string home =
    Environment.GetFolderPath(
        Environment.SpecialFolder.UserProfile);

string testRoot =
    Path.Combine(
        home,
        "CleopatraLoaderTests");

string[] candidatePaths =
{
    Path.Combine(testRoot, "Windower"),
    Path.Combine(testRoot, "Ashita3"),
    Path.Combine(testRoot, "Ashita4")
};

try
{
    // ------------------------------------------------
    // FIND COMPATIBLE LOADERS
    // ------------------------------------------------

    Console.WriteLine(
        "Searching for compatible FFXI loaders...");

    var loaders =
        loaderDetectionService.DetectFromPaths(
            candidatePaths);

    if (loaders.Count == 0)
    {
        Console.WriteLine();
        Console.WriteLine(
            "No compatible Windower or Ashita installation was found.");

        return;
    }

    Console.WriteLine(
        $"Compatible loaders found: {loaders.Count}");

    foreach (var loader in loaders)
    {
        Console.WriteLine(
            $"  - {loader.DisplayName}: {loader.RootPath}");
    }

    // TEMPORARY:
    // Select the first detected loader.
    // The GUI will eventually handle this choice.
    var selectedLoader =
        loaders[0];

    Console.WriteLine();
    Console.WriteLine(
        $"Using: {selectedLoader.DisplayName}");

    // ------------------------------------------------
    // DETECT PIVOT
    // ------------------------------------------------

    bool pivotDetected =
        pivotService.DetectFromLoaderRoot(
            selectedLoader.RootPath);

    if (!pivotDetected)
    {
        Console.WriteLine(
            "Pivot was not found for the selected loader.");

        return;
    }

    var installation =
        pivotService.Installation!;

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

    string cleopatraRoot =
        pivotService.GetCleopatraRoot();

    Console.WriteLine(
        $"Cleopatra directory: {cleopatraRoot}");

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
    // LOAD PREVIOUS INSTALLATION STATE
    // ------------------------------------------------

    var installedManifest =
        installationStateService.Load(
            cleopatraRoot);

    // ------------------------------------------------
    // CHECK AND UPDATE CURRENT FILES
    // ------------------------------------------------

    bool updateSuccessful = true;

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

            updateSuccessful = false;
        }

        Console.WriteLine();
    }

    // ------------------------------------------------
    // STOP IF UPDATE FAILED
    // ------------------------------------------------

    if (!updateSuccessful)
    {
        Console.WriteLine(
            "Update did not complete successfully.");

        Console.WriteLine(
            "Obsolete files were not removed.");

        Console.WriteLine(
            "Installation state was not changed.");

        return;
    }

    // ------------------------------------------------
    // REMOVE OBSOLETE CLEOPATRA FILES
    // ------------------------------------------------

    var obsoleteFiles =
        installationStateService.GetObsoleteFiles(
            installedManifest,
            manifest);

    if (obsoleteFiles.Count > 0)
    {
        Console.WriteLine(
            "Removing obsolete Cleopatra files...");

        foreach (string obsoleteFile in obsoleteFiles)
        {
            string destinationPath =
                pivotService.GetDestinationPath(
                    obsoleteFile);

            bool removed =
                fileUpdater.RemoveObsoleteFile(
                    destinationPath,
                    cleopatraRoot);

            if (removed)
            {
                Console.WriteLine(
                    $"Removed {obsoleteFile}");
            }
            else
            {
                Console.WriteLine(
                    $"Already removed: {obsoleteFile}");
            }
        }

        Console.WriteLine();
    }

    // ------------------------------------------------
    // SAVE INSTALLATION STATE
    // ------------------------------------------------

    installationStateService.Save(
        cleopatraRoot,
        manifest);

    Console.WriteLine(
        $"Installation state saved: {manifest.Version}");

    Console.WriteLine();
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