using System.IO;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Trident.Abstractions;

namespace Polymerium.App
{
    public static class ServiceCollectionExtensions
    {
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
                var dir = PathDef.Default.PrivateDirectory(Program.BRAND);
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
    }
}
