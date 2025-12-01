using System;
using System.Collections.Generic;
using Humanizer;

namespace Polymerium.App.Models;

public class CrashReportModel
{
    // Basic crash info
    public required string InstanceKey { get; init; }
    public required string InstanceName { get; init; }
    public required int ExitCode { get; init; }
    public required DateTimeOffset LaunchTime { get; init; }
    public required DateTimeOffset CrashTime { get; init; }
    public required string ExceptionMessage { get; init; }

    // Game info
    public required string MinecraftVersion { get; init; }
    public required string LoaderLabel { get; init; }
    public required string GameDirectory { get; init; }

    // System info
    public required string OperatingSystem { get; init; }
    public required string JavaVersion { get; init; }
    public required string JavaPath { get; init; }
    public string? AllocatedMemory { get; init; }

    // Log info
    public string? LogFilePath { get; init; }
    public string? CrashReportPath { get; init; }
    public string? LastLogLines { get; init; }

    // Mods info (if applicable)
    public List<string>? InstalledMods { get; init; }
    public int ModCount { get; init; }

    // Process
    public string? CommandLine { get; init; }

    #region Calculated

    public TimeSpan PlayTimeRaw => CrashTime - LaunchTime;
    public string PlayTime => PlayTimeRaw.Humanize(maxUnit: TimeUnit.Day, minUnit: TimeUnit.Second);

    #endregion
}
