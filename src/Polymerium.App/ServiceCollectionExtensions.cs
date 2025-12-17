using System.IO;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using MirrorChyan.Net;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Trident.Abstractions;
using Velopack;
using Velopack.Sources;

namespace Polymerium.App;

public static class ServiceCollectionExtensions
{
    private const string MIRRORCHYAN_PRODUCT_ID = "Polymerium";

    public static IServiceCollection AddAvalonia(this IServiceCollection services)
    {
        services.AddHostedService<AvaloniaLifetime>();
        return services;
    }

    public static IServiceCollection AddViewFacilities(this IServiceCollection services)
    {
        services.AddScoped<ViewBagFactory>().AddScoped<ViewBag>();
        return services;
    }

    public static IServiceCollection AddFreeSql(this IServiceCollection services)
    {
        services.AddSingleton<IFreeSql>(_ =>
        {
            var dir = PathDef.Default.PrivateDirectory(Program.Brand);
            var path = Path.Combine(dir, "persistence.sqlite.db");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return new FreeSqlBuilder()
                  .UseConnectionString(DataType.Sqlite, $"Data Source=\"{path}\";Cache=Private")
                  .UseAutoSyncStructure(true)
                  .Build();
        });
        return services;
    }

    public static IServiceCollection AddSentry(this IServiceCollection services)
    {
        services.AddHostedService<SentryHostedService>();
        return services;
    }

    public static IServiceCollection AddVelopackGithubSource(this IServiceCollection services)
    {
        services.AddSingleton<GithubSource>(_ => new("https://github.com/d3ara1n/Polymerium", null, true));
        services.AddSingleton<IUpdateSource, GithubSource>(sp => sp.GetRequiredService<GithubSource>());
        return services;
    }

    public static IServiceCollection AddVelopack(this IServiceCollection services)
    {
        services.AddSingleton<UpdateSourceSelector>();
        // 不能作为 IUpdateSource 避免自己引用自己
        services.AddSingleton<UpdateManager>(sp => new(sp.GetRequiredService<UpdateSourceSelector>()));
        return services;
    }

    public static IServiceCollection AddMirrorChyan(this IServiceCollection services)
    {
        services.AddMirrorChyan(MIRRORCHYAN_PRODUCT_ID, Program.Brand, Program.Version, new("http://localhost:3000"));
        return services;
    }
}
