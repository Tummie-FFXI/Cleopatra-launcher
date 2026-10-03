using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class LauncherService
{
    private readonly LoaderDetectionService _loaderDetectionService;
    private readonly SettingsService _settingsService;
    private readonly PivotService _pivotService;

    public LauncherService(
        LoaderDetectionService loaderDetectionService,
        SettingsService settingsService,
        PivotService pivotService)
    {
        _loaderDetectionService =
            loaderDetectionService;

        _settingsService =
            settingsService;

        _pivotService =
            pivotService;
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
    // This keeps our Mac development environment usable
    // and will also be useful for manual folder selection.

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
}