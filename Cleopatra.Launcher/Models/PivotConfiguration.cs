namespace Cleopatra.Launcher.Models;

public class PivotConfiguration
{
    public string ConfigurationPath { get; set; } = "";

    public string OverlayRoot { get; set; } = "";

    public List<string> Overlays { get; set; } = new();

    public bool CleopatraEnabled =>
        Overlays.Any(
            overlay => overlay.Equals(
                "Cleopatra",
                StringComparison.OrdinalIgnoreCase));
}