using Cleopatra.Launcher.Models;
using Cleopatra.Launcher.Services;

Console.WriteLine("Cleopatra Launcher");
Console.WriteLine("------------------");

const string manifestUrl =
    "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-updates/main/manifest.json";

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

LoaderProfileService loaderProfileService =
    new LoaderProfileService();

XiLoaderDetectionService xiLoaderDetectionService =
    new XiLoaderDetectionService();

LauncherService launcherService =
    new LauncherService(
        loaderDetectionService,
        settingsService,
        pivotService,
        manifestService,
        fileUpdater,
        installationStateService,
        loaderProfileService,
        xiLoaderDetectionService);

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
    // CLEOPATRA DAT DIRECTORY
    // ------------------------------------------------

    Console.WriteLine();

    string cleopatraRoot =
        launcherService.GetCleopatraRoot();

    Console.WriteLine(
        $"Cleopatra directory: {cleopatraRoot}");

    // ------------------------------------------------
    // UPDATE CLEOPATRA
    // ------------------------------------------------

    bool updateSuccessful =
        await launcherService.UpdateCleopatraAsync(
            manifestUrl);

    if (!updateSuccessful)
    {
        return;
    }

    // ------------------------------------------------
    // LOCATE XILOADER
    // ------------------------------------------------

    Console.WriteLine();
    Console.WriteLine(
        "Locating XiLoader...");

    string? xiLoaderPath =
        launcherService.FindXiLoader();

    if (string.IsNullOrWhiteSpace(xiLoaderPath))
    {
        Console.WriteLine(
            "XiLoader could not be found.");

        Console.WriteLine(
            "A valid xiloader.exe is required to launch Cleopatra.");

        return;
    }

    Console.WriteLine(
        $"XiLoader location: {xiLoaderPath}");

    // ------------------------------------------------
    // PREPARE CLEOPATRA LOADER PROFILE
    // ------------------------------------------------

    launcherService.PrepareLoaderProfile(
        selectedLoader,
        xiLoaderPath);

    Console.WriteLine();
    Console.WriteLine(
        "Cleopatra loader profile ready.");

    // ------------------------------------------------
    // DISPLAY LAUNCH COMMAND
    // ------------------------------------------------
    // DEVELOPMENT TEST ONLY:
    // Verify the command Cleopatra will use without
    // actually attempting to launch the Windows loader
    // on macOS.

    Console.WriteLine();
    Console.WriteLine(
        "Launch command:");

    Console.WriteLine(
        launcherService.GetLaunchDescription(
            selectedLoader));
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine(
        "Cleopatra encountered an error.");

    Console.WriteLine(
        ex.Message);
}