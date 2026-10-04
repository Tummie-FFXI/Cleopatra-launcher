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
    private readonly XiLoaderDetectionService _xiLoaderDetectionService;

    public LauncherService(
        LoaderDetectionService loaderDetectionService,
        SettingsService settingsService,
        PivotService pivotService,
        ManifestService manifestService,
        FileUpdater fileUpdater,
        InstallationStateService installationStateService,
        LoaderProfileService loaderProfileService,
        XiLoaderDetectionService xiLoaderDetectionService)
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

        _xiLoaderDetectionService =
            xiLoaderDetectionService;
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
            .DetectFromPaths(
                candidatePaths);
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
            _pivotService
                .EnsureCleopatraOverlay();
        }

        _pivotService
            .EnsureCleopatraDirectory();

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
    // FIND XILOADER
    // ----------------------------------------------------

    public string? FindXiLoader(
        LoaderInstallation loader)
    {
        return _xiLoaderDetectionService
            .FindXiLoader(
                loader);
    }

    // ----------------------------------------------------
    // PREPARE CLEOPATRA LOADER PROFILE
    // ----------------------------------------------------

    public void PrepareLoaderProfile(
        LoaderInstallation loader,
        string xiLoaderPath)
    {
        _loaderProfileService
            .EnsureCleopatraProfile(
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
            .GetLaunchDescription(
                loader);
    }

    // ----------------------------------------------------
    // LAUNCH CLEOPATRA
    // ----------------------------------------------------

    public void LaunchGame(
        LoaderInstallation loader)
    {
        _loaderProfileService
            .LaunchCleopatraProfile(
                loader);
    }

    // ----------------------------------------------------
    // UPDATE CLEOPATRA
    // ----------------------------------------------------

    public async Task<bool> UpdateCleopatraAsync(
        string manifestUrl,
        IProgress<LauncherUpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string cleopatraRoot =
            GetCleopatraRoot();

        // ------------------------------------------------
        // DOWNLOAD MANIFEST
        // ------------------------------------------------

        Console.WriteLine();
        Console.WriteLine(
            "Checking for updates...");

        progress?.Report(
            new LauncherUpdateProgress(
                Stage: "CheckingManifest"));

        var manifest =
            await _manifestService
                .GetManifestAsync(
                    manifestUrl);

        if (manifest == null)
        {
            Console.WriteLine(
                "Unable to read update manifest.");

            progress?.Report(
                new LauncherUpdateProgress(
                    Stage: "Error"));

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

        progress?.Report(
            new LauncherUpdateProgress(
                Stage: "CheckingFiles",
                TotalFiles:
                    manifest.Files.Count,
                Version:
                    manifest.Version));

        // ------------------------------------------------
        // LOAD PREVIOUS INSTALLATION STATE
        // ------------------------------------------------

        var installedManifest =
            _installationStateService.Load(
                cleopatraRoot);

        // ------------------------------------------------
        // CHECK AND UPDATE CURRENT FILES
        // ------------------------------------------------

        bool updateSuccessful =
            true;

        int totalFiles =
            manifest.Files.Count;

        int currentFileNumber =
            0;

        foreach (var updateFile in manifest.Files)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            currentFileNumber++;

            string destinationPath =
                _pivotService
                    .GetDestinationPath(
                        updateFile.Path);

            string status =
                _fileUpdater
                    .GetFileStatus(
                        updateFile,
                        destinationPath);

            Console.WriteLine(
                $"Checking {updateFile.Path}... {status}");

            progress?.Report(
                new LauncherUpdateProgress(
                    Stage: "CheckingFile",
                    FilePath:
                        updateFile.Path,
                    CurrentFile:
                        currentFileNumber,
                    TotalFiles:
                        totalFiles,
                    Version:
                        manifest.Version));

            if (status == "CURRENT")
            {
                Console.WriteLine();

                continue;
            }

            Console.WriteLine(
                $"Downloading {updateFile.Path}...");

            // --------------------------------------------
            // INDIVIDUAL DAT DOWNLOAD PROGRESS
            // --------------------------------------------

            IProgress<FileDownloadProgress>
                fileProgress =
                    new Progress<FileDownloadProgress>(
                        download =>
                        {
                            progress?.Report(
                                new LauncherUpdateProgress(
                                    Stage:
                                        "Downloading",
                                    FilePath:
                                        updateFile.Path,
                                    CurrentFile:
                                        currentFileNumber,
                                    TotalFiles:
                                        totalFiles,
                                    FilePercent:
                                        download.Percent,
                                    Version:
                                        manifest.Version));
                        });

            bool success =
                await _fileUpdater
                    .DownloadAndVerifyAsync(
                        updateFile,
                        destinationPath,
                        fileProgress,
                        cancellationToken);

            if (success)
            {
                Console.WriteLine(
                    $"Updated {updateFile.Path} successfully.");

                progress?.Report(
                    new LauncherUpdateProgress(
                        Stage:
                            "FileComplete",
                        FilePath:
                            updateFile.Path,
                        CurrentFile:
                            currentFileNumber,
                        TotalFiles:
                            totalFiles,
                        FilePercent:
                            100.0,
                        Version:
                            manifest.Version));
            }
            else
            {
                Console.WriteLine(
                    $"ERROR: {updateFile.Path} failed SHA-256 verification.");

                Console.WriteLine(
                    "The existing file was not changed.");

                progress?.Report(
                    new LauncherUpdateProgress(
                        Stage:
                            "FileError",
                        FilePath:
                            updateFile.Path,
                        CurrentFile:
                            currentFileNumber,
                        TotalFiles:
                            totalFiles,
                        Version:
                            manifest.Version));

                updateSuccessful =
                    false;
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

            progress?.Report(
                new LauncherUpdateProgress(
                    Stage:
                        "Error",
                    TotalFiles:
                        totalFiles,
                    Version:
                        manifest.Version));

            return false;
        }

        // ------------------------------------------------
        // REMOVE OBSOLETE CLEOPATRA FILES
        // ------------------------------------------------

        var obsoleteFiles =
            _installationStateService
                .GetObsoleteFiles(
                    installedManifest,
                    manifest);

        if (obsoleteFiles.Count > 0)
        {
            Console.WriteLine(
                "Removing obsolete Cleopatra files...");

            progress?.Report(
                new LauncherUpdateProgress(
                    Stage:
                        "RemovingObsolete",
                    TotalFiles:
                        totalFiles,
                    Version:
                        manifest.Version));

            foreach (string obsoleteFile in obsoleteFiles)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string destinationPath =
                    _pivotService
                        .GetDestinationPath(
                            obsoleteFile);

                bool removed =
                    _fileUpdater
                        .RemoveObsoleteFile(
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

        progress?.Report(
            new LauncherUpdateProgress(
                Stage:
                    "Complete",
                CurrentFile:
                    totalFiles,
                TotalFiles:
                    totalFiles,
                FilePercent:
                    100.0,
                Version:
                    manifest.Version));

        return true;
    }
}

// ----------------------------------------------------
// LAUNCHER UPDATE PROGRESS
// ----------------------------------------------------
//
// Reports the overall Cleopatra update state to
// consumers such as the WPF GUI.
//
// Stage examples:
//
// CheckingManifest
// CheckingFiles
// CheckingFile
// Downloading
// FileComplete
// FileError
// RemovingObsolete
// Complete
// Error
//
// ----------------------------------------------------

public sealed record LauncherUpdateProgress(
    string Stage,
    string? FilePath = null,
    int CurrentFile = 0,
    int TotalFiles = 0,
    double? FilePercent = null,
    string? Version = null);