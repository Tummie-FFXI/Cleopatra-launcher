using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class PivotService
{
    private PivotInstallation? _installation;

    public PivotInstallation? Installation =>
        _installation;

    public bool DetectFromLoaderRoot(string loaderRoot)
    {
        string fullRoot =
            Path.GetFullPath(loaderRoot);

        // ------------------------------------------
        // WINDOWER 4
        // ------------------------------------------

        string windowerPivot =
            Path.Combine(
                fullRoot,
                "addons",
                "XIPivot");

        if (Directory.Exists(windowerPivot))
        {
            string datRoot =
                Path.Combine(
                    windowerPivot,
                    "data",
                    "DATs");

            _installation =
                new PivotInstallation
                {
                    LoaderType =
                        LoaderType.Windower4,

                    LoaderRoot =
                        fullRoot,

                    PivotRoot =
                        windowerPivot,

                    DatRoot =
                        datRoot,

                    CleopatraRoot =
                        Path.Combine(
                            datRoot,
                            "Cleopatra")
                };

            return true;
        }

        // ------------------------------------------
        // ASHITA 4
        // ------------------------------------------

        string ashita4Pivot =
            Path.Combine(
                fullRoot,
                "polplugins",
                "pivot.dll");

        if (File.Exists(ashita4Pivot))
        {
            string datRoot =
                Path.Combine(
                    fullRoot,
                    "polplugins",
                    "DATs");

            _installation =
                new PivotInstallation
                {
                    LoaderType =
                        LoaderType.Ashita4,

                    LoaderRoot =
                        fullRoot,

                    PivotRoot =
                        Path.Combine(
                            fullRoot,
                            "polplugins"),

                    DatRoot =
                        datRoot,

                    CleopatraRoot =
                        Path.Combine(
                            datRoot,
                            "Cleopatra")
                };

            return true;
        }

        // ------------------------------------------
        // ASHITA 3
        // ------------------------------------------

        string ashita3Pivot =
            Path.Combine(
                fullRoot,
                "plugins",
                "XIPivot");

        if (Directory.Exists(ashita3Pivot))
        {
            string datRoot =
                Path.Combine(
                    ashita3Pivot,
                    "DATs");

            _installation =
                new PivotInstallation
                {
                    LoaderType =
                        LoaderType.Ashita3,

                    LoaderRoot =
                        fullRoot,

                    PivotRoot =
                        ashita3Pivot,

                    DatRoot =
                        datRoot,

                    CleopatraRoot =
                        Path.Combine(
                            datRoot,
                            "Cleopatra")
                };

            return true;
        }

        _installation = null;

        return false;
    }

    public string GetCleopatraRoot()
    {
        if (_installation == null)
        {
            throw new InvalidOperationException(
                "No Pivot installation has been detected.");
        }

        return _installation.CleopatraRoot;
    }

    public void EnsureCleopatraDirectory()
    {
        Directory.CreateDirectory(
            GetCleopatraRoot());
    }

    public string GetDestinationPath(
        string relativePath)
    {
        string cleopatraRoot =
            Path.GetFullPath(
                GetCleopatraRoot());

        string destinationPath =
            Path.GetFullPath(
                Path.Combine(
                    cleopatraRoot,
                    relativePath));

        string rootWithSeparator =
            cleopatraRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!destinationPath.StartsWith(
            rootWithSeparator,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid manifest path.");
        }

        return destinationPath;
    }
}