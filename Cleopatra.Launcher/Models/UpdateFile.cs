namespace Cleopatra.Launcher.Models;

public class UpdateFile
{
    public string Path { get; set; } = "";

    public string Url { get; set; } = "";

    public string Sha256 { get; set; } = "";

    public string Group { get; set; } = "";

    public string Note { get; set; } = "";
}