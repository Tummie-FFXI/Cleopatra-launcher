using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class LoaderDetectionService
{
    public List<LoaderInstallation> DetectFromPaths(
        IEnumerable<string> candidatePaths)
    {
        List<LoaderInstallation> installations =
            new();

        foreach (string candidatePath in candidatePaths)
        {
            if (!Directory.Exists(candidatePath))
            {
                continue;
            }

            LoaderInstallation? installation =
                DetectLoader(candidatePath);

            if (installation != null)
            {
                installations.Add(
                    installation);
            }
        }

        return installations;
    }

    private static LoaderInstallation? DetectLoader(
        string rootPath)
    {
        string fullRoot =
            Path.GetFullPath(rootPath);

        // --------------------------------------------
        // WINDOWER 4
        // --------------------------------------------

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

        // --------------------------------------------
        // ASHITA 4
        // --------------------------------------------

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

        // --------------------------------------------
        // ASHITA 3
        // --------------------------------------------

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