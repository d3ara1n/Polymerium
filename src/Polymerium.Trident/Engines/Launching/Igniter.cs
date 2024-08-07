﻿using IBuilder;
using System.Diagnostics;
using System.Drawing;

namespace Polymerium.Trident.Engines.Launching;

public class Igniter : IBuilder<Process>
{
    public IList<string> GameArguments { get; } = new List<string>();
    public IList<string> JvmArguments { get; } = new List<string>();
    public IList<string> Libraries { get; } = new List<string>();
    public string? WorkingDirectory { get; set; }
    public string? AssetRootDirectory { get; set; }
    public string? LibraryRootDirectory { get; set; }
    public string? NativeRootDirectory { get; set; }
    public string? MainClass { get; set; }
    public string? JavaHome { get; set; }
    public string? AssetIndex { get; set; }
    public string? UserName { get; set; }
    public string? VersionName { get; set; }
    public string? ReleaseType { get; set; }
    public string? UserUuid { get; set; }
    public string? UserAccessToken { get; set; }
    public string? UserType { get; set; }
    public string? OsName { get; set; }
    public string? OsArch { get; set; }
    public string? OsVersion { get; set; }
    public Size? WindowSize { get; set; }
    public string? LauncherName { get; set; }
    public string? LauncherVersion { get; set; }
    public uint? MaxMemory { get; set; }
    public bool IsDebug { get; set; }
    public char? ClassPathSeparator { get; set; }

    public Process Build()
    {
        var classPath = string.Join(ClassPathSeparator!.Value, Libraries);
        Dictionary<string, string> crates = new()
        {
            { "${auth_player_name}", UserName! },
            { "${version_name}", VersionName! },
            { "${game_directory}", WorkingDirectory! },
            { "${assets_root}", AssetRootDirectory! },
            { "${assets_index_name}", AssetIndex! },
            { "${auth_uuid}", UserUuid! },
            { "${auth_access_token}", UserAccessToken! },
            { "${user_type}", UserType! },
            { "${version_type}", ReleaseType! },
            { "${resolution_width}", WindowSize!.Value.Width.ToString() },
            { "${resolution_height}", WindowSize!.Value.Height.ToString() },
            { "${natives_directory}", NativeRootDirectory! },
            { "${library_directory}", LibraryRootDirectory! },
            { "${launcher_name}", LauncherName! },
            { "${launcher_version}", LauncherVersion! },
            { "${os_name}", OsName! },
            { "${os_arch}", OsArch! },
            { "${os_version}", OsVersion! },
            { "${jvm_max_memory}", $"{MaxMemory!.Value}m" },
            { "${classpath_separator}", ClassPathSeparator!.ToString()! },
            { "${classpath}", classPath }
        };
        var excecutable = Path.Combine(JavaHome!, "bin", IsDebug ? "java.exe" : "javaw.exe");
        ProcessStartInfo start = new(excecutable) { WorkingDirectory = WorkingDirectory!, UseShellExecute = IsDebug };
        foreach (var argument in JvmArguments.Where(x => !string.IsNullOrEmpty(x)))
        {
            var crate = crates.FirstOrDefault(x => argument.Contains(x.Key));
            var line = crate.Key == null || crate.Value == null
                ? argument
                : argument.Replace(crate.Key, crate.Value);
            start.ArgumentList.Add(line);
        }

        start.ArgumentList.Add(MainClass!);
        foreach (var argument in GameArguments.Where(x => !string.IsNullOrEmpty(x)))
        {
            var line = crates.TryGetValue(argument, out var value) ? value : argument;
            start.ArgumentList.Add(line);
        }

        Process process = new() { StartInfo = start };
        return process;
    }

    public Igniter SetMainClass(string mainClass)
    {
        MainClass = mainClass;
        return this;
    }

    public Igniter SetJavaHome(string path)
    {
        JavaHome = path;
        return this;
    }

    public Igniter Debug()
    {
        IsDebug = true;
        return this;
    }

    public Igniter SetWorkingDirectory(string directory)
    {
        WorkingDirectory = directory;
        return this;
    }

    public Igniter SetAssetRootDirectory(string directory)
    {
        AssetRootDirectory = directory;
        return this;
    }

    public Igniter SetNativetRootDirectory(string directory)
    {
        NativeRootDirectory = directory;
        return this;
    }

    public Igniter SetLibraryRootDirectory(string directory)
    {
        LibraryRootDirectory = directory;
        return this;
    }

    public Igniter SetClassPathSeparator(char separator)
    {
        ClassPathSeparator = separator;
        return this;
    }

    public Igniter SetAssetIndex(string index)
    {
        AssetIndex = index;
        return this;
    }

    public Igniter SetUserName(string name)
    {
        UserName = name;
        return this;
    }

    public Igniter SetVersionName(string name)
    {
        VersionName = name;
        return this;
    }

    public Igniter SetReleaseType(string type)
    {
        ReleaseType = type;
        return this;
    }

    public Igniter SetUserUuid(string uuid)
    {
        UserUuid = uuid;
        return this;
    }

    public Igniter SetUserAccessToken(string accessToken)
    {
        UserAccessToken = accessToken;
        return this;
    }

    public Igniter SetUserType(string type)
    {
        UserType = type;
        return this;
    }

    public Igniter SetOsName(string name)
    {
        OsName = name;
        return this;
    }

    public Igniter SetOsArch(string arch)
    {
        OsArch = arch;
        return this;
    }

    public Igniter SetOsVersion(string version)
    {
        OsVersion = version;
        return this;
    }

    public Igniter SetWindowSize(Size size)
    {
        WindowSize = size;
        return this;
    }

    public Igniter SetLauncherName(string name)
    {
        LauncherName = name;
        return this;
    }

    public Igniter SetLauncherVersion(string version)
    {
        LauncherVersion = version;
        return this;
    }

    public Igniter SetMaxMemory(uint maxMemory)
    {
        MaxMemory = maxMemory;
        return this;
    }

    public Igniter AddGameArgument(string argument)
    {
        GameArguments.Add(argument);
        return this;
    }

    public Igniter AddJvmArgument(string argument)
    {
        JvmArguments.Add(argument);
        return this;
    }

    public Igniter AddLibrary(string path)
    {
        Libraries.Add(path);
        return this;
    }
}