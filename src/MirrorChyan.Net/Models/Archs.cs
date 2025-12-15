using System.Reflection;
using System.Runtime.InteropServices;

namespace MirrorChyan.Net.Models;

public static class Archs
{
    public const string _386 = "386";
    public const string X86 = "x86";
    public const string X86_32 = "x86_32";
    public const string I386 = "i386";

    public const string Amd64 = "amd64";
    public const string X86_64 = "x86_64";
    public const string Intel64 = "intel64";

    public const string Arm = "arm";

    public const string Arm64 = "arm64";
    public const string Aarch64 = "aarch64";

    public static readonly string[] X86s = [X86_32, X86, _386, I386];
    public static readonly string[] X64s = [X86_64, Amd64, Intel64];
    public static readonly string[] Arm32s = [Arm];
    public static readonly string[] Arm64s = [Arm64, Aarch64];

    public static bool IsX86(string arch) => X86s.Contains(arch);
    public static bool IsX64(string arch) => X64s.Contains(arch);
    public static bool IsArm32(string arch) => Arm32s.Contains(arch);
    public static bool IsArm64(string arch) => Arm64s.Contains(arch);


    public static string FromPlatform() =>
        RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => X86,
            Architecture.X64 => Amd64,
            Architecture.Arm => Arm,
            Architecture.Arm64 => Arm64,
            _ => throw new NotSupportedException("Unsupported process architecture.")
        };

    public static Architecture ToPlatform(string arch)
    {
        if (IsX86(arch))
        {
            return Architecture.X86;
        }

        if (IsX64(arch))
        {
            return Architecture.X64;
        }

        if (IsArm32(arch))
        {
            return Architecture.Arm;
        }

        if (IsArm64(arch))
        {
            return Architecture.Arm64;
        }

        throw new ArgumentOutOfRangeException(nameof(arch), arch, null);
    }
}
