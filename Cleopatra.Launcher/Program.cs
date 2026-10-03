using Cleopatra.Launcher.Models;
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

SettingsService settingsService =
    new SettingsService();

ManifestService manifestService =
    new ManifestService(client);

FileUpdater fileUpdater =
    new FileUpdater(client);

PivotService pivotService =
    new PivotService();

InstallationStateService installationStateService =
    new InstallationStateService();

try
{
    // ------------------------------------------------
    // LOAD SAVED LOADER
    // ------------------------------------------------

    Console.WriteLine(
        "Checking saved loader configuration...");

    LauncherSettings settings =
        settingsService.Load();

    LoaderInstallation? selectedLoader = null;

    if (settings.LoaderType != LoaderType.Unknown &&
        !string.IsNullOrWhiteSpace(settings.LoaderPath))
    {
        var savedLoaderResults =
            loaderDetectionService.DetectFromPaths(
                new[]
                {
                    settings.LoaderPath
                });

        selectedLoader =
            savedLoaderResults.FirstOrDefault(
                loader =>
                    loader.LoaderType ==
                    settings.LoaderType);

        if (selectedLoader != null)
        {
            Console.WriteLine(
                $"Using saved loader: {selectedLoader.DisplayName}");

            Console.WriteLine(
                $"Loader location: {selectedLoader.RootPath}");
        }
        else
        {
            Console.WriteLine(
                "Saved loader is no longer available.");

            Console.WriteLine(
                "Searching for compatible loaders...");
        }
    }

    // ------------------------------------------------
    // DISCOVER LOADERS IF NECESSARY
    // ------------------------------------------------

    if (selectedLoader == null)
    {
        List<LoaderInstallation> loaders;

        if (OperatingSystem.IsWindows())
        {
            loaders =
                loaderDetectionService
                    .DetectWindowsInstallations();
        }
        else
        {
            // ----------------------------------------
            // TEMPORARY MAC DEVELOPMENT PATHS
            // ----------------------------------------

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

            loaders =
                loaderDetectionService.DetectFromPaths(
                    candidatePaths);
        }

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
        // Until the GUI selection screen exists,
        // use the first discovered loader.
        selectedLoader =
            loaders[0];

        Console.WriteLine();
        Console.WriteLine(
            $"Selected loader: {selectedLoader.DisplayName}");

        // Save the selection for future launches.

        settings =
            new LauncherSettings
            {
                LoaderType =
                    selectedLoader.LoaderType,

                LoaderPath =
                    selectedLoader.RootPath
            };

        settingsService.Save(
            settings);

        Console.WriteLine(
            "Loader selection saved.");
    }

    // ------------------------------------------------
    // DETECT PIVOT
    // ------------------------------------------------

    Console.WriteLine();
    Console.WriteLine(
        "Checking Pivot installation...");

    bool pivotDetected =
        pivotService.DetectFromLoaderRoot(
            selectedLoader.RootPath);

    if (!pivotDetected)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Pivot was not found for {selectedLoader.DisplayName}.");

        Console.WriteLine(
            "Pivot is required to use Cleopatra custom DAT files.");

        Console.WriteLine(
            "Please install Pivot for your loader and restart Cleopatra.");

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