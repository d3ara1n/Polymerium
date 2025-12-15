using MirrorChyan.Net.Models;
using Velopack;

namespace VelopackExtension.MirrorChyan;

/// <summary>
/// Velopack 架构与 MirrorChyan 架构之间的转换辅助类
/// </summary>
public static class VelopackArchs
{
    /// <summary>
    /// 将 Velopack 的运行时架构转换为 MirrorChyan API 使用的架构字符串
    /// </summary>
    /// <param name="arch">Velopack 运行时架构</param>
    /// <returns>MirrorChyan 架构字符串</returns>
    /// <exception cref="NotSupportedException">不支持的架构</exception>
    public static string FromVelopack(VelopackRuntimeArch arch) => arch switch
    {
        VelopackRuntimeArch.x86 => Archs.X86,
        VelopackRuntimeArch.x64 => Archs.Amd64,
        VelopackRuntimeArch.arm64 => Archs.Arm64,
        _ => throw new NotSupportedException($"Unsupported Velopack architecture: {arch}")
    };

    /// <summary>
    /// 将 MirrorChyan 架构字符串转换为 Velopack 运行时架构
    /// </summary>
    /// <param name="arch">MirrorChyan 架构字符串</param>
    /// <returns>Velopack 运行时架构</returns>
    /// <exception cref="ArgumentOutOfRangeException">无法识别的架构字符串</exception>
    public static VelopackRuntimeArch ToVelopack(string arch)
    {
        if (Archs.IsX86(arch))
        {
            return VelopackRuntimeArch.x86;
        }

        if (Archs.IsX64(arch))
        {
            return VelopackRuntimeArch.x64;
        }

        if (Archs.IsArm64(arch))
        {
            return VelopackRuntimeArch.arm64;
        }

        throw new ArgumentOutOfRangeException(nameof(arch), arch, "Unknown architecture string");
    }
}
