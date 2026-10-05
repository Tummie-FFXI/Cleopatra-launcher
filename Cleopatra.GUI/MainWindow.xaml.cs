using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Cleopatra.Launcher.Models;
using Cleopatra.Launcher.Services;

namespace Cleopatra.GUI;

public partial class MainWindow : Window
{
    // ----------------------------------------------------
    // MANIFEST
    // ----------------------------------------------------

    private const string ManifestUrl =
        "https://raw.githubusercontent.com/Tummie-FFXI/Cleopatra-updates/main/manifest.json";

    // ----------------------------------------------------
    // SERVICES
    // ----------------------------------------------------

    private readonly HttpClient _httpClient;

    private readonly LauncherService _launcherService;

    // ----------------------------------------------------
    // CURRENT LOADER
    // ----------------------------------------------------

    private LoaderInstallation? _selectedLoader;

    // ----------------------------------------------------
    // UPDATE STATE
    // ----------------------------------------------------

    private bool _updateInProgress;

    private bool _detailsVisible;

    // ----------------------------------------------------
    // CONSTRUCTOR
    // ----------------------------------------------------

    public MainWindow()
    {
        InitializeComponent();

        // --------------------------------------------
        // CREATE SERVICES
        // --------------------------------------------

        _httpClient =
            new HttpClient();

        LoaderDetectionService loaderDetectionService =
            new LoaderDetectionService();

        SettingsService settingsService =
            new SettingsService();

        ManifestService manifestService =
            new ManifestService(
                _httpClient);

        FileUpdater fileUpdater =
            new FileUpdater(
                _httpClient);

        PivotService pivotService =
            new PivotService();

        InstallationStateService installationStateService =
            new InstallationStateService();

        LoaderProfileService loaderProfileService =
            new LoaderProfileService();

        XiLoaderDetectionService xiLoaderDetectionService =
            new XiLoaderDetectionService();

        _launcherService =
            new LauncherService(
                loaderDetectionService,
                settingsService,
                pivotService,
                manifestService,
                fileUpdater,
                installationStateService,
                loaderProfileService,
                xiLoaderDetectionService);

        // --------------------------------------------
        // GUI EVENTS
        // --------------------------------------------

        Loaded +=
            MainWindow_Loaded;

        PlayButton.Click +=
            PlayButton_Click;

        UpdateDetailsButton.Click +=
            UpdateDetailsButton_Click;

        RetryUpdateButton.Click +=
            RetryUpdateButton_Click;

        Closed +=
            MainWindow_Closed;

        // PLAY XI remains unavailable until the
        // launcher has successfully initialized.

        PlayButton.IsEnabled =
            false;
    }

    // ----------------------------------------------------
    // WINDOW LOADED
    // ----------------------------------------------------

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await InitializeLauncherAsync();
    }

    // ----------------------------------------------------
    // INITIALIZE LAUNCHER
    // ----------------------------------------------------

    private async Task InitializeLauncherAsync()
    {
        if (_updateInProgress)
        {
            return;
        }

        _updateInProgress =
            true;

        try
        {
            ShowCheckingState(
                "Checking launcher configuration...");

            // --------------------------------------------
            // GET SAVED LOADER
            // --------------------------------------------

            _selectedLoader =
                _launcherService.GetSavedLoader();

            // --------------------------------------------
            // DISCOVER LOADER IF NO SAVED LOADER EXISTS
            // --------------------------------------------

            if (_selectedLoader == null)
            {
                ShowCheckingState(
                    "Searching for Windower or Ashita...");

                List<LoaderInstallation> loaders =
                    _launcherService
                        .DiscoverWindowsLoaders();

                if (loaders.Count == 0)
                {
                    ShowErrorState(
                        "No compatible Windower 4 or Ashita 4 installation was found.");

                    return;
                }

                // TEMPORARY:
                //
                // Until the Settings / first-run loader
                // selection screen is implemented,
                // use the first compatible loader found.

                _selectedLoader =
                    loaders[0];

                _launcherService.SaveLoader(
                    _selectedLoader);
            }

            // --------------------------------------------
            // CHECK PIVOT
            // --------------------------------------------

            ShowCheckingState(
                $"Checking Pivot for {_selectedLoader.DisplayName}...");

            bool pivotReady =
                _launcherService.PreparePivot(
                    _selectedLoader);

            if (!pivotReady)
            {
                ShowErrorState(
                    $"Pivot was not found for {_selectedLoader.DisplayName}.");

                return;
            }

            // --------------------------------------------
            // LOCATE XILOADER
            // --------------------------------------------

            string? xiLoaderPath =
                _launcherService.FindXiLoader(
                    _selectedLoader);

            if (string.IsNullOrWhiteSpace(xiLoaderPath))
            {
                ShowErrorState(
                    "XiLoader could not be found. Check your Windower configuration.");

                return;
            }

            // --------------------------------------------
            // PREPARE CLEOPATRA PROFILE
            // --------------------------------------------

            ShowCheckingState(
                $"Preparing {_selectedLoader.DisplayName}...");

            _launcherService.PrepareLoaderProfile(
                _selectedLoader,
                xiLoaderPath);

            // --------------------------------------------
            // CHECK / UPDATE CLEOPATRA FILES
            // --------------------------------------------

            ShowCheckingState(
                "Checking Cleopatra files...");

            IProgress<LauncherUpdateProgress> progress =
                new Progress<LauncherUpdateProgress>(
                    HandleUpdateProgress);

            bool updateSuccessful =
                await _launcherService
                    .UpdateCleopatraAsync(
                        ManifestUrl,
                        progress);

            if (!updateSuccessful)
            {
                ShowErrorState(
                    "Cleopatra could not complete the update.");

                return;
            }

            // --------------------------------------------
            // READY
            // --------------------------------------------

            ShowReadyState();
        }
        catch (Exception ex)
        {
            ShowErrorState(
                ex.Message);
        }
        finally
        {
            _updateInProgress =
                false;
        }
    }

    // ----------------------------------------------------
    // HANDLE UPDATE PROGRESS
    // ----------------------------------------------------

    private void HandleUpdateProgress(
        LauncherUpdateProgress update)
    {
        switch (update.Stage)
        {
            // --------------------------------------------
            // MANIFEST
            // --------------------------------------------

            case "CheckingManifest":

                ShowCheckingState(
                    "Checking for updates...");

                break;

            // --------------------------------------------
            // CHECKING FILES
            // --------------------------------------------

            case "CheckingFiles":

                ShowCheckingState(
                    "Checking Cleopatra files...");

                if (!string.IsNullOrWhiteSpace(
                    update.Version))
                {
                    UpdateStatusMeta.Text =
                        $"Version {update.Version}";
                }

                break;

            // --------------------------------------------
            // CHECKING INDIVIDUAL FILE
            // --------------------------------------------

            case "CheckingFile":

                ShowCheckingState(
                    $"Checking {update.FilePath}...");

                if (update.TotalFiles > 0)
                {
                    UpdateStatusMeta.Text =
                        $"{update.CurrentFile} of {update.TotalFiles}";
                }

                break;

            // --------------------------------------------
            // DOWNLOADING
            // --------------------------------------------

            case "Downloading":

                ShowUpdatingState(
                    update);

                break;

            // --------------------------------------------
            // FILE COMPLETE
            // --------------------------------------------

            case "FileComplete":

                ShowUpdatingState(
                    update);

                break;

            // --------------------------------------------
            // FILE ERROR
            // --------------------------------------------

            case "FileError":

                ShowErrorState(
                    $"Update failed: {update.FilePath}");

                break;

            // --------------------------------------------
            // REMOVING OBSOLETE FILES
            // --------------------------------------------

            case "RemovingObsolete":

                ShowCheckingState(
                    "Cleaning up old Cleopatra files...");

                break;

            // --------------------------------------------
            // COMPLETE
            // --------------------------------------------

            case "Complete":

                ShowReadyState(
                    update.Version);

                break;

            // --------------------------------------------
            // ERROR
            // --------------------------------------------

            case "Error":

                ShowErrorState(
                    "Cleopatra update failed.");

                break;
        }
    }

    // ----------------------------------------------------
    // CHECKING STATE
    // ----------------------------------------------------

    private void ShowCheckingState(
        string message)
    {
        PlayButton.IsEnabled =
            false;

        UpdateStatusIcon.Text =
            "◌";

        UpdateStatusIcon.Foreground =
            new SolidColorBrush(
                Color.FromRgb(
                    184,
                    154,
                    99));

        UpdateStatusText.Text =
            message;

        UpdateStatusMeta.Visibility =
            Visibility.Visible;

        RetryUpdateButton.Visibility =
            Visibility.Collapsed;

        UpdateDetailsButton.Visibility =
            Visibility.Collapsed;

        UpdateProgressPanel.Visibility =
            Visibility.Collapsed;

        UpdateFileDetailsPanel.Visibility =
            Visibility.Collapsed;

        _detailsVisible =
            false;
    }

    // ----------------------------------------------------
    // UPDATING STATE
    // ----------------------------------------------------

    private void ShowUpdatingState(
        LauncherUpdateProgress update)
    {
        PlayButton.IsEnabled =
            false;

        UpdateStatusIcon.Text =
            "↓";

        UpdateStatusIcon.Foreground =
            new SolidColorBrush(
                Color.FromRgb(
                    184,
                    154,
                    99));

        UpdateStatusText.Text =
            "Updating Cleopatra";

        // --------------------------------------------
        // FILE NUMBER
        // --------------------------------------------

        if (update.TotalFiles > 0)
        {
            UpdateStatusMeta.Text =
                $"{update.CurrentFile} of {update.TotalFiles}";
        }
        else
        {
            UpdateStatusMeta.Text =
                "Updating";
        }

        UpdateStatusMeta.Visibility =
            Visibility.Visible;

        // --------------------------------------------
        // CURRENT FILE METADATA
        // --------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            update.Group))
        {
            CurrentFileGroupText.Text =
                update.Group.ToUpperInvariant();

            CurrentFileGroupText.Visibility =
                Visibility.Visible;
        }
        else
        {
            CurrentFileGroupText.Text =
                "CLEOPATRA FILE";

            CurrentFileGroupText.Visibility =
                Visibility.Visible;
        }

        if (!string.IsNullOrWhiteSpace(
            update.Note))
        {
            CurrentFileNoteText.Text =
                update.Note;
        }
        else if (!string.IsNullOrWhiteSpace(
            update.FilePath))
        {
            CurrentFileNoteText.Text =
                update.FilePath;
        }
        else
        {
            CurrentFileNoteText.Text =
                "Downloading...";
        }

        CurrentFilePathText.Text =
            update.FilePath ??
            "Unknown file";

        // --------------------------------------------
        // PERCENT
        // --------------------------------------------

        if (update.FilePercent.HasValue)
        {
            double percent =
                Math.Clamp(
                    update.FilePercent.Value,
                    0.0,
                    100.0);

            UpdatePercentText.Text =
                $"{percent:0}%";

            ProgressCompletedColumn.Width =
                new GridLength(
                    percent,
                    GridUnitType.Star);

            ProgressRemainingColumn.Width =
                new GridLength(
                    100.0 - percent,
                    GridUnitType.Star);
        }
        else
        {
            UpdatePercentText.Text =
                "...";

            ProgressCompletedColumn.Width =
                new GridLength(
                    0,
                    GridUnitType.Star);

            ProgressRemainingColumn.Width =
                new GridLength(
                    100,
                    GridUnitType.Star);
        }

        // --------------------------------------------
        // SHOW UPDATE AREA
        // --------------------------------------------

        UpdateProgressPanel.Visibility =
            Visibility.Visible;

        UpdateDetailsButton.Visibility =
            Visibility.Visible;

        RetryUpdateButton.Visibility =
            Visibility.Collapsed;
    }

    // ----------------------------------------------------
    // READY STATE
    // ----------------------------------------------------

    private void ShowReadyState(
        string? version = null)
    {
        PlayButton.IsEnabled =
            true;

        UpdateStatusIcon.Text =
            "✓";

        UpdateStatusIcon.Foreground =
            new SolidColorBrush(
                Color.FromRgb(
                    123,
                    178,
                    135));

        UpdateStatusText.Text =
            "Cleopatra is up to date";

        if (!string.IsNullOrWhiteSpace(version))
        {
            UpdateStatusMeta.Text =
                $"Version {version}";
        }

        UpdateStatusMeta.Visibility =
            Visibility.Visible;

        UpdateProgressPanel.Visibility =
            Visibility.Collapsed;

        UpdateDetailsButton.Visibility =
            Visibility.Collapsed;

        RetryUpdateButton.Visibility =
            Visibility.Collapsed;

        UpdateFileDetailsPanel.Visibility =
            Visibility.Collapsed;

        _detailsVisible =
            false;
    }

    // ----------------------------------------------------
    // ERROR STATE
    // ----------------------------------------------------

    private void ShowErrorState(
        string message)
    {
        PlayButton.IsEnabled =
            false;

        UpdateStatusIcon.Text =
            "!";

        UpdateStatusIcon.Foreground =
            new SolidColorBrush(
                Color.FromRgb(
                    199,
                    121,
                    111));

        UpdateStatusText.Text =
            message;

        UpdateStatusMeta.Visibility =
            Visibility.Collapsed;

        UpdateProgressPanel.Visibility =
            Visibility.Collapsed;

        UpdateDetailsButton.Visibility =
            Visibility.Collapsed;

        RetryUpdateButton.Visibility =
            Visibility.Visible;

        UpdateFileDetailsPanel.Visibility =
            Visibility.Collapsed;

        _detailsVisible =
            false;
    }

    // ----------------------------------------------------
    // DETAILS BUTTON
    // ----------------------------------------------------

    private void UpdateDetailsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _detailsVisible =
            !_detailsVisible;

        if (_detailsVisible)
        {
            UpdateFileDetailsPanel.Visibility =
                Visibility.Visible;

            UpdateDetailsButton.Content =
                "Details ▴";
        }
        else
        {
            UpdateFileDetailsPanel.Visibility =
                Visibility.Collapsed;

            UpdateDetailsButton.Content =
                "Details ▾";
        }
    }

    // ----------------------------------------------------
    // RETRY UPDATE
    // ----------------------------------------------------

    private async void RetryUpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await InitializeLauncherAsync();
    }

    // ----------------------------------------------------
    // PLAY XI
    // ----------------------------------------------------

    private void PlayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedLoader == null)
        {
            ShowErrorState(
                "No loader is configured.");

            return;
        }

        try
        {
            _launcherService.LaunchGame(
                _selectedLoader);
        }
        catch (Exception ex)
        {
            ShowErrorState(
                $"Unable to launch FFXI: {ex.Message}");
        }
    }

    // ----------------------------------------------------
    // WINDOW DRAG
    // ----------------------------------------------------

    private void Header_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();

            return;
        }

        if (e.ButtonState ==
            MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    // ----------------------------------------------------
    // MINIMIZE
    // ----------------------------------------------------

    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState.Minimized;
    }

    // ----------------------------------------------------
    // MAXIMIZE / RESTORE
    // ----------------------------------------------------

    private void MaximizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        if (WindowState ==
            WindowState.Maximized)
        {
            WindowState =
                WindowState.Normal;
        }
        else
        {
            WindowState =
                WindowState.Maximized;
        }
    }

    // ----------------------------------------------------
    // CLOSE
    // ----------------------------------------------------

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    // ----------------------------------------------------
    // WINDOW CLOSED
    // ----------------------------------------------------

    private void MainWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _httpClient.Dispose();
    }
}