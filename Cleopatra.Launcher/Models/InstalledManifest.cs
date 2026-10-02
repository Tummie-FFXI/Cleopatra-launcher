namespace Cleopatra.Launcher.Models;

public class InstalledManifest
{
    public string Version { get; set; } = "";

    public List<string> Files { get; set; } = new();
}