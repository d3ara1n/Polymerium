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

    #region Nested type: $extension

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAvalonia()
        {
            services.AddHostedService<AvaloniaLifetime>();
            return services;
        }

        public IServiceCollection AddViewFacilities()
        {
            services.AddScoped<ViewBagFactory>().AddScoped<ViewBag>();
            return services;
        }

        public IServiceCollection AddFreeSql()
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

        public IServiceCollection AddSentry()
        {
            services.AddHostedService<SentryHostedService>();
            return services;
        }

        public IServiceCollection AddVelopackGithubSource()
        {
            services.AddSingleton<GithubSource>(_ => new("https://github.com/d3ara1n/Polymerium", null, true));
            services.AddSingleton<IUpdateSource, GithubSource>(sp => sp.GetRequiredService<GithubSource>());
            return services;
        }

        public IServiceCollection AddVelopack()
        {
            services.AddSingleton<UpdateSourceSelector>();
            // 不能作为 IUpdateSource 避免自己引用自己
            services.AddSingleton<UpdateManager>(sp => new(sp.GetRequiredService<UpdateSourceSelector>()));
            return services;
        }

        public IServiceCollection AddMirrorChyan()
        {
            services.AddMirrorChyan(MIRRORCHYAN_PRODUCT_ID, Program.Brand, Program.Version);
            return services;
        }
    }

    #endregion
}
