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

LauncherService launcherService =
    new LauncherService(
        loaderDetectionService,
        settingsService,
        pivotService);

try
{
    // ------------------------------------------------
    // GET SAVED LOADER
    // ------------------------------------------------

    Console.WriteLine(
        "Checking saved loader configuration...");

    LoaderInstallation? selectedLoader =
        launcherService.GetSavedLoader();

    if (selectedLoader != null)
    {
        Console.WriteLine(
            $"Using saved loader: {selectedLoader.DisplayName}");

        Console.WriteLine(
            $"Loader location: {selectedLoader.RootPath}");
    }

    // ------------------------------------------------
    // DISCOVER LOADERS IF NECESSARY
    // ------------------------------------------------

    if (selectedLoader == null)
    {
        Console.WriteLine(
            "Searching for compatible loaders...");

        List<LoaderInstallation> loaders;

        if (OperatingSystem.IsWindows())
        {
            loaders =
                launcherService.DiscoverWindowsLoaders();
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
                Path.Combine(testRoot, "Ashita4")
            };

            loaders =
                launcherService.DiscoverLoaders(
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

        launcherService.SaveLoader(
            selectedLoader);

        Console.WriteLine(
            "Loader selection saved.");
    }

    // ------------------------------------------------
    // PREPARE PIVOT
    // ------------------------------------------------

    Console.WriteLine();
    Console.WriteLine(
        "Checking Pivot installation...");

    bool pivotReady =
        launcherService.PreparePivot(
            selectedLoader);

    if (!pivotReady)
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

    PivotInstallation? installation =
        launcherService.GetPivotInstallation();

    if (installation == null)
    {
        Console.WriteLine(
            "Unable to read Pivot installation information.");

        return;
    }

    Console.WriteLine(
        $"Pivot location: {installation.PivotRoot}");

    string cleopatraRoot =
        launcherService.GetCleopatraRoot();

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