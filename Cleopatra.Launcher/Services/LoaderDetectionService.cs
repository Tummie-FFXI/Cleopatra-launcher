using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class LoaderDetectionService
{
    // ----------------------------------------------------
    // DETECT FROM PROVIDED PATHS
    // ----------------------------------------------------

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

        return DetectFromPaths(
            GetWindowsCandidatePaths());
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

        // Windower

        AddCandidate(
            paths,
            Path.Combine(userProfile, "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(localAppData, "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(appData, "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(programFiles, "Windower4"));

        AddCandidate(
            paths,
            Path.Combine(programFilesX86, "Windower4"));

        // Ashita

        AddCandidate(
            paths,
            Path.Combine(userProfile, "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(userProfile, "Ashita4"));

        AddCandidate(
            paths,
            Path.Combine(localAppData, "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(localAppData, "Ashita4"));

        AddCandidate(
            paths,
            Path.Combine(programFiles, "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(programFiles, "Ashita4"));

        AddCandidate(
            paths,
            Path.Combine(programFilesX86, "Ashita"));

        AddCandidate(
            paths,
            Path.Combine(programFilesX86, "Ashita4"));

        return paths;
    }

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
    //
    // IMPORTANT:
    // This only identifies the loader.
    // It does NOT require Pivot to be installed.
    //
    // PivotService is responsible for Pivot detection.

    private static LoaderInstallation? DetectLoader(
        string rootPath)
    {
        string fullRoot =
            Path.GetFullPath(rootPath);

        // ------------------------------------------------
        // WINDOWER 4
        // ------------------------------------------------

        bool looksLikeWindower =
            File.Exists(
                Path.Combine(
                    fullRoot,
                    "Windower.exe"))
            ||
            Directory.Exists(
                Path.Combine(
                    fullRoot,
                    "addons"));

        if (looksLikeWindower)
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

        bool looksLikeAshita4 =
            Directory.Exists(
                Path.Combine(
                    fullRoot,
                    "polplugins"))
            ||
            Directory.Exists(
                Path.Combine(
                    fullRoot,
                    "config"));

        if (looksLikeAshita4)
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

        bool looksLikeAshita3 =
            Directory.Exists(
                Path.Combine(
                    fullRoot,
                    "plugins"));

        if (looksLikeAshita3)
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