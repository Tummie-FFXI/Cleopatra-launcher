using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class LoaderDetectionService
{
    // ----------------------------------------------------
    // DETECT FROM PROVIDED PATHS
    // ----------------------------------------------------
    // Used by our Mac development tests and later by
    // manual "Browse for Loader Folder" selection.

    public List<LoaderInstallation> DetectFromPaths(
        IEnumerable<string> candidatePaths)
    {
        List<LoaderInstallation> installations =
            new();

        HashSet<string> detectedPaths =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string candidatePath in candidatePaths)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                continue;
            }

            if (!Directory.Exists(candidatePath))
            {
                continue;
            }

            LoaderInstallation? installation =
                DetectLoader(candidatePath);

            if (installation == null)
            {
                continue;
            }

            // Prevent the same installation from appearing
            // more than once if multiple candidate paths
            // resolve to the same directory.
            if (detectedPaths.Add(
                installation.RootPath))
            {
                installations.Add(
                    installation);
            }
        }

        return installations;
    }

    // ----------------------------------------------------
    // DETECT WINDOWS INSTALLATIONS
    // ----------------------------------------------------

    public List<LoaderInstallation> DetectWindowsInstallations()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new List<LoaderInstallation>();
        }

        List<string> candidatePaths =
            GetWindowsCandidatePaths();

        return DetectFromPaths(
            candidatePaths);
    }

    // ----------------------------------------------------
    // BUILD WINDOWS CANDIDATE PATH LIST
    // ----------------------------------------------------

    private static List<string> GetWindowsCandidatePaths()
    {
        List<string> paths =
            new();

        string userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string appData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

        string programFiles =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles);

        string programFilesX86 =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86);

        // ------------------------------------------------
        // COMMON WINDOWER LOCATIONS
        // ------------------------------------------------

        AddCandidate(
            paths,
            Path.Combine(
                userProfile,
                "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(
                localAppData,
                "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(
                appData,
                "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(
                programFiles,
                "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(
                programFilesX86,
                "Windower4"));

        // ------------------------------------------------
        // COMMON ASHITA LOCATIONS
        // ------------------------------------------------

        AddCandidate(
            paths,
            Path.Combine(
                userProfile,
                "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(
                userProfile,
                "Ashita4"));

        AddCandidate(
            paths,
            Path.Combine(
                localAppData,
                "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(
                localAppData,
                "Ashita4"));

        AddCandidate(
            paths,
            Path.Combine(
                programFiles,
                "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(
                programFiles,
                "Ashita4"));

        AddCandidate(
            paths,
            Path.Combine(
                programFilesX86,
                "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(
                programFilesX86,
                "Ashita4"));

        return paths;
    }

    // ----------------------------------------------------
    // ADD CANDIDATE
    // ----------------------------------------------------

    private static void AddCandidate(
        List<string> paths,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!paths.Contains(
            path,
            StringComparer.OrdinalIgnoreCase))
        {
            paths.Add(path);
        }
    }

    // ----------------------------------------------------
    // IDENTIFY LOADER
    // ----------------------------------------------------

    private static LoaderInstallation? DetectLoader(
        string rootPath)
    {
        string fullRoot =
            Path.GetFullPath(rootPath);

        // ------------------------------------------------
        // WINDOWER 4
        // ------------------------------------------------

        string windowerPivot =
            Path.Combine(
                fullRoot,
                "addons",
                "XIPivot");

        if (Directory.Exists(windowerPivot))
        {
            return new LoaderInstallation
            {
                LoaderType =
                    LoaderType.Windower4,

                RootPath =
                    fullRoot,

                DisplayName =
                    "Windower 4"
            };
        }

        // ------------------------------------------------
        // ASHITA 4
        // ------------------------------------------------

        string ashita4Pivot =
            Path.Combine(
                fullRoot,
                "polplugins",
                "pivot.dll");

        if (File.Exists(ashita4Pivot))
        {
            return new LoaderInstallation
            {
                LoaderType =
                    LoaderType.Ashita4,

                RootPath =
                    fullRoot,

                DisplayName =
                    "Ashita v4"
            };
        }

        // ------------------------------------------------
        // ASHITA 3
        // ------------------------------------------------

        string ashita3Pivot =
            Path.Combine(
                fullRoot,
                "plugins",
                "XIPivot");

        if (Directory.Exists(ashita3Pivot))
        {
            return new LoaderInstallation
            {
                LoaderType =
                    LoaderType.Ashita3,

                RootPath =
                    fullRoot,

                DisplayName =
                    "Ashita v3"
            };
        }

        return null;
    }
}