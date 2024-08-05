using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polymerium.Cli.Commands;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Profiles;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trident.Abstractions.Repositories;

namespace Polymerium.Cli;

internal static class Program
{
    // 针对实例和本地文件的管理
    // trident list
    // trident run $:instance
    // trident inspect $:instance
    // trident clean $:instance
    // trident deploy $:instance
    // trident remove $:instance
    // 针对 instance 管理
    // trident instance create --version {}
    // trident instance import
    // 针对某个 instance 的元数据管理(M=component,attachment,layer)
    // trident M add --instance {} $:purl|id
    // trident M remove --instance {} $:purl|id
    // trident M list --instance {}
    // trident attachment enable/disable --instance {} $:purl
    // 在线数据源仓库管理
    // trident repository list
    // trident repository ping
    // 在线资源查询
    // trident resource search --repository {} --take {} --skip --filters {{}} {} $:keyword
    // trident resource resolve $:purl

    // deploy
    // 第一步 Flatten，根据 Metadata 构建出 Polylock
    // 这一步需要解析所有资源文件和依赖并导出，以便下次可以直接仅检查文件完整性即可启动游戏
    // 第二步 Restore，即检查并补全文件到 Polylock 状态
    private static int Main(string[] args)
    {
        var trident = new TridentContext(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".trident"));
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ProfileConverter());

        var services = new ServiceCollection();

        services
            .AddSingleton(options)
            .AddHttpClient()
            .ConfigureHttpClientDefaults(builder => builder.RemoveAllLoggers().ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent",
                        $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
                })
                .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync()))
            .AddLogging(builder => builder.AddDebug());

        services
            .AddSingleton(trident);
        services
            .AddSingleton<RepositoryAgent>()
            .AddSingleton<DownloadManager>()
            .AddSingleton<ModpackExtractor>()
            .AddSingleton<StorageManager>()
            .AddSingleton<ThumbnailSaver>()
            .AddSingleton<InstanceManager>()
            .AddSingleton<AccountManager>();

        services
            .AddTransient<IRepository, CurseForgeRepository>()
            .AddTransient<IRepository, ModrinthRepository>();

        var app = new CommandApp(new TypeRegistrar(services));
        app.Configure(configure =>
        {
            configure.SetExceptionHandler((ex, _) =>
                AnsiConsole.Markup("[bold red]{0}:[/] {1}", ex.GetType().Name, ex.Message));
            configure
                .AddCommand<ListCommand>("list");
            configure
                .AddCommand<InspectCommand>("inspect")
                .WithExample("inspect", "--instance", "my_profile");
            configure
                .AddBranch("repository", branch =>
                {
                    branch
                        .AddCommand<RepositoryListCommand>("list");
                });
            configure
                .AddBranch("resource", branch =>
                {
                    branch
                        .AddCommand<ResourceSearchCommand>("search");
                    branch
                        .AddCommand<ResourceQueryCommand>("query");
                });
        });

        return app.Run(args);
    }
}