using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class LauncherService
{
    private readonly LoaderDetectionService _loaderDetectionService;
    private readonly SettingsService _settingsService;
    private readonly PivotService _pivotService;
    private readonly ManifestService _manifestService;
    private readonly FileUpdater _fileUpdater;
    private readonly InstallationStateService _installationStateService;
    private readonly LoaderProfileService _loaderProfileService;

    public LauncherService(
        LoaderDetectionService loaderDetectionService,
        SettingsService settingsService,
        PivotService pivotService,
        ManifestService manifestService,
        FileUpdater fileUpdater,
        InstallationStateService installationStateService,
        LoaderProfileService loaderProfileService)
    {
        _loaderDetectionService =
            loaderDetectionService;

        _settingsService =
            settingsService;

        _pivotService =
            pivotService;

        _manifestService =
            manifestService;

        _fileUpdater =
            fileUpdater;

        _installationStateService =
            installationStateService;

        _loaderProfileService =
            loaderProfileService;
    }

    // ----------------------------------------------------
    // GET SAVED LOADER
    // ----------------------------------------------------

    public LoaderInstallation? GetSavedLoader()
    {
        LauncherSettings settings =
            _settingsService.Load();

        if (settings.LoaderType == LoaderType.Unknown ||
            string.IsNullOrWhiteSpace(settings.LoaderPath))
        {
            return null;
        }

        var results =
            _loaderDetectionService.DetectFromPaths(
                new[]
                {
                    settings.LoaderPath
                });

        return results.FirstOrDefault(
            loader =>
                loader.LoaderType ==
                settings.LoaderType);
    }

    // ----------------------------------------------------
    // SAVE LOADER
    // ----------------------------------------------------

    public void SaveLoader(
        LoaderInstallation loader)
    {
        LauncherSettings settings =
            new LauncherSettings
            {
                LoaderType =
                    loader.LoaderType,

                LoaderPath =
                    loader.RootPath
            };

        _settingsService.Save(
            settings);
    }

    // ----------------------------------------------------
    // DISCOVER WINDOWS LOADERS
    // ----------------------------------------------------

    public List<LoaderInstallation>
        DiscoverWindowsLoaders()
    {
        return _loaderDetectionService
            .DetectWindowsInstallations();
    }

    // ----------------------------------------------------
    // DISCOVER PROVIDED PATHS
    // ----------------------------------------------------
    // Used by the Mac development environment and later
    // by manual loader-folder selection.

    public List<LoaderInstallation> DiscoverLoaders(
        IEnumerable<string> candidatePaths)
    {
        return _loaderDetectionService
            .DetectFromPaths(candidatePaths);
    }

    // ----------------------------------------------------
    // PREPARE PIVOT
    // ----------------------------------------------------

    public bool PreparePivot(
        LoaderInstallation loader)
    {
        bool detected =
            _pivotService.DetectFromLoaderRoot(
                loader.RootPath);

        if (!detected)
        {
            return false;
        }

        var configuration =
            _pivotService.ReadConfiguration();

        if (!configuration.CleopatraEnabled)
        {
            _pivotService.EnsureCleopatraOverlay();
        }

        _pivotService.EnsureCleopatraDirectory();

        return true;
    }

    // ----------------------------------------------------
    // CLEOPATRA ROOT
    // ----------------------------------------------------

    public string GetCleopatraRoot()
    {
        return _pivotService
            .GetCleopatraRoot();
    }

    // ----------------------------------------------------
    // PIVOT INSTALLATION
    // ----------------------------------------------------

    public PivotInstallation? GetPivotInstallation()
    {
        return _pivotService.Installation;
    }

    // ----------------------------------------------------
    // PREPARE CLEOPATRA LOADER PROFILE
    // ----------------------------------------------------

    public void PrepareLoaderProfile(
        LoaderInstallation loader,
        string xiLoaderPath)
    {
        _loaderProfileService.EnsureCleopatraProfile(
            loader,
            xiLoaderPath);
    }

    // ----------------------------------------------------
    // GET LAUNCH DESCRIPTION
    // ----------------------------------------------------

    public string GetLaunchDescription(
        LoaderInstallation loader)
    {
        return _loaderProfileService
            .GetLaunchDescription(loader);
    }

    // ----------------------------------------------------
    // LAUNCH CLEOPATRA
    // ----------------------------------------------------

    public void LaunchGame(
        LoaderInstallation loader)
    {
        _loaderProfileService.LaunchCleopatraProfile(
            loader);
    }

    // ----------------------------------------------------
    // UPDATE CLEOPATRA
    // ----------------------------------------------------

    public async Task<bool> UpdateCleopatraAsync(
        string manifestUrl)
    {
        string cleopatraRoot =
            GetCleopatraRoot();

        // ------------------------------------------------
        // DOWNLOAD MANIFEST
        // ------------------------------------------------

        Console.WriteLine();
        Console.WriteLine(
            "Checking for updates...");

        var manifest =
            await _manifestService.GetManifestAsync(
                manifestUrl);

        if (manifest == null)
        {
            Console.WriteLine(
                "Unable to read update manifest.");

            return false;
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
            _installationStateService.Load(
                cleopatraRoot);

        // ------------------------------------------------
        // CHECK AND UPDATE CURRENT FILES
        // ------------------------------------------------

        bool updateSuccessful = true;

        foreach (var updateFile in manifest.Files)
        {
            string destinationPath =
                _pivotService.GetDestinationPath(
                    updateFile.Path);

            string status =
                _fileUpdater.GetFileStatus(
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
                await _fileUpdater.DownloadAndVerifyAsync(
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

            return false;
        }

        // ------------------------------------------------
        // REMOVE OBSOLETE CLEOPATRA FILES
        // ------------------------------------------------

        var obsoleteFiles =
            _installationStateService.GetObsoleteFiles(
                installedManifest,
                manifest);

        if (obsoleteFiles.Count > 0)
        {
            Console.WriteLine(
                "Removing obsolete Cleopatra files...");

            foreach (string obsoleteFile in obsoleteFiles)
            {
                string destinationPath =
                    _pivotService.GetDestinationPath(
                        obsoleteFile);

                bool removed =
                    _fileUpdater.RemoveObsoleteFile(
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

        _installationStateService.Save(
            cleopatraRoot,
            manifest);

        Console.WriteLine(
            $"Installation state saved: {manifest.Version}");

        Console.WriteLine();
        Console.WriteLine(
            "Update check complete.");

        return true;
    }
}