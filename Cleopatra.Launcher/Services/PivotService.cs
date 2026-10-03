using System.Xml.Linq;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class PivotService
{
    private PivotInstallation? _installation;

    public PivotInstallation? Installation =>
        _installation;

    // ----------------------------------------------------
    // DETECT PIVOT INSTALLATION
    // ----------------------------------------------------

    public bool DetectFromLoaderRoot(string loaderRoot)
    {
        string fullRoot =
            Path.GetFullPath(loaderRoot);

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
            string datRoot =
                Path.Combine(
                    windowerPivot,
                    "data",
                    "DATs");

            string configPath =
                Path.Combine(
                    windowerPivot,
                    "data",
                    "settings.xml");

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
                            "Cleopatra"),

                    ConfigurationPath =
                        configPath
                };

            return true;
        }

        // ------------------------------------------------
        // ASHITA 4
        // ------------------------------------------------

        string ashita4Dll =
            Path.Combine(
                fullRoot,
                "polplugins",
                "pivot.dll");

        if (File.Exists(ashita4Dll))
        {
            string defaultDatRoot =
                Path.Combine(
                    fullRoot,
                    "polplugins",
                    "DATs");

            string configPath =
                Path.Combine(
                    fullRoot,
                    "config",
                    "pivot",
                    "pivot.ini");

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
                        defaultDatRoot,

                    CleopatraRoot =
                        Path.Combine(
                            defaultDatRoot,
                            "Cleopatra"),

                    ConfigurationPath =
                        configPath
                };

            return true;
        }

        _installation = null;

        return false;
    }

    // ----------------------------------------------------
    // READ PIVOT CONFIGURATION
    // ----------------------------------------------------

    public PivotConfiguration ReadConfiguration()
    {
        if (_installation == null)
        {
            throw new InvalidOperationException(
                "No Pivot installation has been detected.");
        }

        return _installation.LoaderType switch
        {
            LoaderType.Windower4 =>
                ReadWindowerConfiguration(),

            LoaderType.Ashita4 =>
                ReadAshita4Configuration(),

            _ =>
                new PivotConfiguration
                {
                    ConfigurationPath =
                        _installation.ConfigurationPath,

                    OverlayRoot =
                        _installation.DatRoot
                }
        };
    }

    // ----------------------------------------------------
    // WINDOWER CONFIGURATION
    // ----------------------------------------------------

    private PivotConfiguration ReadWindowerConfiguration()
    {
        string configPath =
            _installation!.ConfigurationPath;

        PivotConfiguration configuration =
            new PivotConfiguration
            {
                ConfigurationPath =
                    configPath,

                OverlayRoot =
                    _installation.DatRoot
            };

        if (!File.Exists(configPath))
        {
            return configuration;
        }

        XDocument document =
            XDocument.Load(configPath);

        XElement? overlaysElement =
            document
                .Descendants("overlays")
                .FirstOrDefault();

        if (overlaysElement == null)
        {
            return configuration;
        }

        string overlayText =
            overlaysElement.Value;

        configuration.Overlays =
            overlayText
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList();

        return configuration;
    }

    // ----------------------------------------------------
    // ASHITA 4 CONFIGURATION
    // ----------------------------------------------------

    private PivotConfiguration ReadAshita4Configuration()
    {
        string configPath =
            _installation!.ConfigurationPath;

        PivotConfiguration configuration =
            new PivotConfiguration
            {
                ConfigurationPath =
                    configPath,

                OverlayRoot =
                    _installation.DatRoot
            };

        if (!File.Exists(configPath))
        {
            return configuration;
        }

        string currentSection = "";

        foreach (string rawLine in File.ReadLines(configPath))
        {
            string line =
                rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith(";") ||
                line.StartsWith("#"))
            {
                continue;
            }

            if (line.StartsWith("[") &&
                line.EndsWith("]"))
            {
                currentSection =
                    line[1..^1].Trim();

                continue;
            }

            int equalsIndex =
                line.IndexOf('=');

            if (equalsIndex < 0)
            {
                continue;
            }

            string key =
                line[..equalsIndex].Trim();

            string value =
                line[(equalsIndex + 1)..].Trim();

            // Read a custom Pivot root_path.
            if (currentSection.Equals(
                    "settings",
                    StringComparison.OrdinalIgnoreCase) &&
                key.Equals(
                    "root_path",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(value))
            {
                string rootPath =
                    value;

                // Resolve relative root_path values
                // relative to the Ashita root.
                if (!Path.IsPathRooted(rootPath))
                {
                    rootPath =
                        Path.GetFullPath(
                            Path.Combine(
                                _installation.LoaderRoot,
                                rootPath));
                }

                configuration.OverlayRoot =
                    rootPath;
            }

            // Read enabled overlays.
            if (currentSection.Equals(
                "overlays",
                StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    configuration.Overlays.Add(
                        value);
                }
            }
        }

        // Respect Ashita's configured root_path.
        _installation.DatRoot =
            configuration.OverlayRoot;

        _installation.CleopatraRoot =
            Path.Combine(
                configuration.OverlayRoot,
                "Cleopatra");

        return configuration;
    }

    // ----------------------------------------------------
    // ENABLE CLEOPATRA OVERLAY
    // ----------------------------------------------------

    public void EnsureCleopatraOverlay()
    {
        if (_installation == null)
        {
            throw new InvalidOperationException(
                "No Pivot installation has been detected.");
        }

        PivotConfiguration configuration =
            ReadConfiguration();

        // Don't modify anything if Cleopatra
        // is already enabled.
        if (configuration.CleopatraEnabled)
        {
            return;
        }

        switch (_installation.LoaderType)
        {
            case LoaderType.Windower4:
                EnableWindowerOverlay();
                break;

            case LoaderType.Ashita4:
                EnableAshita4Overlay();
                break;

            default:
                throw new NotSupportedException(
                    $"Automatic Pivot configuration is not yet supported for {_installation.LoaderType}.");
        }
    }

    // ----------------------------------------------------
    // ENABLE WINDOWER OVERLAY
    // ----------------------------------------------------

    private void EnableWindowerOverlay()
    {
        string configPath =
            _installation!.ConfigurationPath;

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                "Windower XIPivot settings.xml was not found.",
                configPath);
        }

        XDocument document =
            XDocument.Load(configPath);

        XElement? overlaysElement =
            document
                .Descendants("overlays")
                .FirstOrDefault();

        if (overlaysElement == null)
        {
            throw new InvalidOperationException(
                "The Windower XIPivot configuration does not contain an overlays setting.");
        }

        List<string> overlays =
            overlaysElement.Value
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList();

        bool alreadyEnabled =
            overlays.Any(
                overlay =>
                    overlay.Equals(
                        "Cleopatra",
                        StringComparison.OrdinalIgnoreCase));

        if (!alreadyEnabled)
        {
            overlays.Add("Cleopatra");
        }

        overlaysElement.Value =
            string.Join(",", overlays);

        document.Save(configPath);
    }

    // ----------------------------------------------------
    // ENABLE ASHITA 4 OVERLAY
    // ----------------------------------------------------

    private void EnableAshita4Overlay()
    {
        string configPath =
            _installation!.ConfigurationPath;

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                "Ashita Pivot configuration was not found.",
                configPath);
        }

        List<string> lines =
            File.ReadAllLines(configPath)
                .ToList();

        int overlaysSectionIndex = -1;
        int nextSectionIndex = lines.Count;

        // Locate [overlays].
        for (int i = 0; i < lines.Count; i++)
        {
            string line =
                lines[i].Trim();

            if (line.Equals(
                "[overlays]",
                StringComparison.OrdinalIgnoreCase))
            {
                overlaysSectionIndex = i;

                // Find the beginning of the next INI section.
                for (int j = i + 1;
                     j < lines.Count;
                     j++)
                {
                    string nextLine =
                        lines[j].Trim();

                    if (nextLine.StartsWith("[") &&
                        nextLine.EndsWith("]"))
                    {
                        nextSectionIndex = j;
                        break;
                    }
                }

                break;
            }
        }

        // No [overlays] section exists.
        if (overlaysSectionIndex < 0)
        {
            if (lines.Count > 0 &&
                !string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.Add("");
            }

            lines.Add("[overlays]");
            lines.Add("0=Cleopatra");
        }
        else
        {
            int highestIndex = -1;

            for (int i = overlaysSectionIndex + 1;
                 i < nextSectionIndex;
                 i++)
            {
                string line =
                    lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line) ||
                    line.StartsWith(";") ||
                    line.StartsWith("#"))
                {
                    continue;
                }

                int equalsIndex =
                    line.IndexOf('=');

                if (equalsIndex < 0)
                {
                    continue;
                }

                string key =
                    line[..equalsIndex].Trim();

                string value =
                    line[(equalsIndex + 1)..].Trim();

                // Cleopatra is already enabled.
                if (value.Equals(
                    "Cleopatra",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (int.TryParse(
                    key,
                    out int index))
                {
                    highestIndex =
                        Math.Max(
                            highestIndex,
                            index);
                }
            }

            // Add Cleopatra after the player's
            // existing overlays.
            lines.Insert(
                nextSectionIndex,
                $"{highestIndex + 1}=Cleopatra");
        }

        File.WriteAllLines(
            configPath,
            lines);
    }

    // ----------------------------------------------------
    // CLEOPATRA DIRECTORY
    // ----------------------------------------------------

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

    // ----------------------------------------------------
    // SAFE DESTINATION PATH
    // ----------------------------------------------------

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

        // Prevent manifest paths such as ../../
        // from escaping Cleopatra's overlay directory.
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