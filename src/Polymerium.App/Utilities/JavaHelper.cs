using System;
using System.IO;
using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Core.Exceptions;
using Trident.Core.Services.Instances;

namespace Polymerium.App.Utilities;

public static class JavaHelper
{
    public static JavaHomeLocatorDelegate MakeLocator(
        Profile first,
        Configuration secondary,
        bool withFallback = true) =>
        major => Locate(major, first, secondary, withFallback);

    private static string Locate(uint major, Profile first, Configuration secondary, bool withFallback = true)
    {
        var home = first.GetOverride(Profile.OVERRIDE_JAVA_HOME,
                                     major switch
                                     {
                                         8 => secondary.RuntimeJavaHome8,
                                         11 => secondary.RuntimeJavaHome11,
                                         16 or 17 => secondary.RuntimeJavaHome17,
                                         21 => secondary.RuntimeJavaHome21,
                                         _ => throw new ArgumentOutOfRangeException(nameof(major),
                                                  major,
                                                  $"Unsupported java version: {major}")
                                     });
        if (!string.IsNullOrEmpty(home) && Directory.Exists(home))
        {
            return home;
        }

        if (withFallback)
        {
            var dir = PathDef.Default.DirectoryOfRuntime(major);
            var path = Path.Combine(dir, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");
            if (File.Exists(path))
            {
                return dir;
            }
        }

        throw new JavaNotFoundException(major);
    }
}
