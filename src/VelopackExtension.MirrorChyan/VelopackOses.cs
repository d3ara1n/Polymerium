using MirrorChyan.Net.Models;
using Velopack;

namespace VelopackExtension.MirrorChyan;

/// <summary>
/// Velopack 操作系统与 MirrorChyan 操作系统之间的转换辅助类
/// </summary>
public static class VelopackOses
{
    /// <summary>
    /// 将 Velopack 的运行时操作系统转换为 MirrorChyan API 使用的操作系统字符串
    /// </summary>
    /// <param name="os">Velopack 运行时操作系统</param>
    /// <returns>MirrorChyan 操作系统字符串</returns>
    /// <exception cref="NotSupportedException">不支持的操作系统</exception>
    public static string FromVelopack(VelopackRuntimeOs os) => os switch
    {
        VelopackRuntimeOs.Windows => Oses.Windows,
        VelopackRuntimeOs.Linux => Oses.Linux,
        VelopackRuntimeOs.OSX => Oses.Darwin,
        _ => throw new NotSupportedException($"Unsupported Velopack OS: {os}")
    };

    /// <summary>
    /// 将 MirrorChyan 操作系统字符串转换为 Velopack 运行时操作系统
    /// </summary>
    /// <param name="os">MirrorChyan 操作系统字符串</param>
    /// <returns>Velopack 运行时操作系统</returns>
    /// <exception cref="ArgumentOutOfRangeException">无法识别的操作系统字符串</exception>
    public static VelopackRuntimeOs ToVelopack(string os)
    {
        if (Oses.IsWindows(os))
        {
            return VelopackRuntimeOs.Windows;
        }

        if (Oses.IsLinux(os))
        {
            return VelopackRuntimeOs.Linux;
        }

        if (Oses.IsDarwin(os))
        {
            return VelopackRuntimeOs.OSX;
        }

        throw new ArgumentOutOfRangeException(nameof(os), os, "Unknown OS string");
    }
}
