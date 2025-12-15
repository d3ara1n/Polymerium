using Microsoft.Extensions.DependencyInjection;
using MirrorChyan.Net;
using MirrorChyan.Net.Clients;
using Velopack;

namespace VelopackExtension.MirrorChyan;

/// <summary>
/// 用于 MirrorChyan Velopack 集成的依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 MirrorChyan Velopack 更新源支持
    /// 此方法会自动调用 AddMirrorChyan 注册 MirrorChyan 基础服务，
    /// 并注册使用 MirrorChyanSource 的 UpdateManager
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="productName">产品名称（MirrorChyan 资源 ID）</param>
    /// <param name="currentVersion">当前应用版本</param>
    /// <param name="cdk">CDK 密钥（可选）</param>
    /// <param name="userAgent">自定义 User-Agent（可选）</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddMirrorChyanVelopack(
        this IServiceCollection services,
        string productName,
        string currentVersion,
        string? cdk = null,
        string? userAgent = null)
    {
        // 1. 注册 MirrorChyan 基础服务（包括 IMirrorChyanClient）
        services.AddMirrorChyan(productName, currentVersion);

        // 2. 注册 UpdateManager（使用 MirrorChyanSource）
        services.AddSingleton<UpdateManager>(sp =>
        {
            var client = sp.GetRequiredService<IMirrorChyanClient>();
            var source = new MirrorChyanSource(
                client,
                productName,
                currentVersion,
                cdk,
                userAgent);

            return new UpdateManager(source);
        });

        return services;
    }
}
