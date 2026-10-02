namespace Cleopatra.Launcher.Models;

public class UpdateManifest
{
    public string Version { get; set; } = "";

    public List<UpdateFile> Files { get; set; } = new();
}