using Microsoft.Extensions.DependencyInjection;
using Velopack;
using Velopack.Sources;
using VelopackExtension.MirrorChyan.Sources;

namespace VelopackExtension.MirrorChyan;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 MirrorChyan 作为 Velopack 更新源
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置 MirrorChyanSourceOptions 的委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddVelopackMirrorChyan(
        this IServiceCollection services,
        Action<MirrorChyanSourceOptions>? configureOptions = null)
    {
        // 注册选项
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // 注册 MirrorChyanSource
        services.AddSingleton<MirrorChyanSource>();
        services.AddSingleton<IUpdateSource>(sp => sp.GetRequiredService<MirrorChyanSource>());

        // 注册 UpdateManager
        services.AddSingleton<UpdateManager>(sp =>
        {
            var source = sp.GetRequiredService<MirrorChyanSource>();
            return new UpdateManager(source);
        });

        return services;
    }
}
