using System.Text.Json.Serialization;

namespace Cleopatra.Launcher.Models;

public class LauncherSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LoaderType LoaderType { get; set; } =
        LoaderType.Unknown;

    public string LoaderPath { get; set; } = "";
}