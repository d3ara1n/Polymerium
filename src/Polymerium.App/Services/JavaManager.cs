using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Polymerium.Abstractions;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

public class JavaManager
{
    private readonly Regex keyValueCompiled = new("^(?<key>[A-Z_]+)=\"(?<value>[a-z\\ A-Z0-9_.-]*)\"$");

    public IEnumerable<string> QueryJavaInstallations()
    {
        var list = new List<string>();
        var candidates = new[]
        {
            ("SOFTWARE\\JavaSoft\\Java Runtime Environment", null, "JavaHome"),
            ("SOFTWARE\\JavaSoft\\Java Development Kit", null, "JavaHome"),
            ("SOFTWARE\\JavaSoft\\JRE", null, "JavaHome"),
            ("SOFTWARE\\JavaSoft\\JDK", null, "JavaHome"),
            ("SOFTWARE\\AdoptOpenJDK\\JRE", "hotspot\\MSI", "Path"),
            ("SOFTWARE\\AdoptOpenJDK\\JDK", "hotspot\\MSI", "Path"),
            ("SOFTWARE\\Eclipse Foundation\\JDK", "hotspot\\MSI", "Path"),
            ("SOFTWARE\\Eclipse Adoptium\\JRE", "hotspot\\MSI", "Path"),
            ("SOFTWARE\\Eclipse Adoptium\\JDK", "hotspot\\MSI", "Path"),
            ("SOFTWARE\\Microsoft\\JDK", "hotspot\\MSI", "Path"),
            ("SOFTWARE\\Azul Systems\\Zulu", null, "InstallationPath"),
            ("SOFTWARE\\BellSoft\\Liberica", null, "InstallationPath")
        };
        foreach (var (path, subPath, key) in candidates)
        {
            var items = SearchInRegistry(path, subPath, key);
            foreach (var item in items) list.Add(item);
        }

        return list;
    }

    public Option<JavaInstallationModel> VerifyJavaHome(string home)
    {
        var releaseFile = Path.Combine(home, "release");
        if (File.Exists(releaseFile))
        {
            var lines = File.ReadAllText(releaseFile).Replace("\r", string.Empty).Split('\n');
            var result = new JavaInstallationModel(home);
            var anyMatched = false;
            foreach (var line in lines)
            {
                var match = keyValueCompiled.Match(line);
                if (match.Success)
                {
                    anyMatched = true;
                    var key = match.Groups["key"];
                    var value = match.Groups["value"];
                    switch (key.Value)
                    {
                        case "IMPLEMENTOR":
                            result.Implementor = value.Value;
                            break;
                        case "JAVA_VERSION":
                            result.JavaVersion = value.Value;
                            break;
                        case "OS_ARCH":
                            result.OsArch = value.Value;
                            break;
                        case "OS_NAME":
                            result.OsName = value.Value;
                            break;
                    }
                }
            }

            if (anyMatched)
            {
                result.Summary = $"{result.JavaVersion} {result.OsName} {result.OsArch} - {result.Implementor}";
                return Option<JavaInstallationModel>.Some(result);
            }

            return Option<JavaInstallationModel>.None();
        }

        return Option<JavaInstallationModel>.None();
    }

    private IEnumerable<string> SearchInRegistry(string path, string? subPath, string key)
    {
        using var r = Registry.LocalMachine.OpenSubKey(path);
        if (r != null)
        {
            var subkeys = r.GetSubKeyNames();
            foreach (var subkey in subkeys)
            {
                using var subR = r.OpenSubKey(string.IsNullOrEmpty(subPath) ? subkey : $"{subkey}\\{subPath}");
                if (subR != null)
                {
                    var value = subR.GetValue(key);
                    if (value is string it) yield return it;
                }
            }
        }
    }
}