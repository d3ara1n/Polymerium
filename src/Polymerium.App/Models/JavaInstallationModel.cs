namespace Polymerium.App.Models;

public class JavaInstallationModel
{
    public JavaInstallationModel(string implementor, string javaVersion, string osArch, string osName, string homePath,
        string summary)
    {
        Implementor = implementor;
        JavaVersion = javaVersion;
        OsArch = osArch;
        OsName = osName;
        HomePath = homePath;
        Summary = summary;
    }

    public JavaInstallationModel(string homePath)
    {
        HomePath = homePath;
    }

    public string? Implementor { get; set; }
    public string? JavaVersion { get; set; }
    public string? OsArch { get; set; }
    public string? OsName { get; set; }
    public string HomePath { get; set; }
    public string? Summary { get; set; }
}