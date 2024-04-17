using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Cli.Commands;

[Description("Search in the specific repository")]
public class ResourceSearchCommand(RepositoryAgent agent) : AsyncCommand<ResourceSearchCommand.Arguments>
{
    private readonly IDictionary<string, string> LOADER_MAPPINGS = new Dictionary<string, string>()
    {
        { "forge", Loader.COMPONENT_FORGE },
        { "neoforge", Loader.COMPONENT_NEOFORGE },
        { "fabric", Loader.COMPONENT_FABRIC },
        { "quilt", Loader.COMPONENT_QUILT }
    };

    public class Arguments : CommandSettings
    {
        [CommandArgument(0, "[query]")] public string? Query { get; init; }
        [CommandOption("-v|--version")] public string? Version { get; init; }
        [CommandOption("-l|--loader")] public string? Loader { get; init; }
        [CommandOption("-r|--repository")] public string? Label { get; init; }
        [CommandOption("-k|--kind")] public string? Kind { get; init; }

        [CommandOption("-p|--page")]
        [DefaultValue(0u)]
        public uint Page { get; init; }

        [CommandOption("-s|--size")]
        [DefaultValue(10u)]
        public uint Size { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Arguments settings)
    {
        var loader = GetLoaderId(settings.Loader);
        var kind = GetResourceKind(settings.Kind);
        var label = settings.Label ?? RepositoryLabels.CURSEFORGE;
        var filter = new Filter(settings.Version, loader, kind);
        var query = settings.Query ?? string.Empty;
        AnsiConsole.Write(new Rule("[bold yellow]SEARCH[/]"));
        AnsiConsole.MarkupLine($"[aqua]Repository:[/] {Markup.Escape(label)}");
        AnsiConsole.MarkupLine($"[aqua]Kind:[/] {kind}");
        AnsiConsole.MarkupLine($"[aqua]Loader[/] {filter.ModLoader ?? "[gray]ANY[/]"}");
        AnsiConsole.MarkupLine($"[aqua]Version[/] {filter.Version ?? "[gray]ANY[/]"}");
        AnsiConsole.MarkupLine($"[aqua]Page:[/] {settings.Page}");
        AnsiConsole.MarkupLine($"[aqua]Size:[/] {settings.Size}");
        AnsiConsole.MarkupLine($"[aqua]Query:[/] {settings.Query ?? "[gray]BLANK[/]"}");


        var results =
            (await agent.SearchAsync(label, query, settings.Page, settings.Size, filter)).ToArray();

        AnsiConsole.Write(new Rule($"[bold yellow]RESULTS({results.Length})[/]"));

        if (results.Length > 0)
            AnsiConsole.Write(
                new Rows(results.Select((x, i) =>
                    new Markup(
                        $"{Markup.Escape($"[{i}]")}([underline red]{Markup.Escape(x.Id)}[/])[blue]{Markup.Escape(x.Name)}[/]: [gray]{Markup.Escape(x.Summary)}[/]"))));
        else
            AnsiConsole.Write(new Rule("[gray]EMPTY[/]").NoBorder());


        return 0;
    }

    private string? GetLoaderId(string? friendly)
    {
        if (friendly == null) return null;
        if (LOADER_MAPPINGS.Values.Contains(friendly)) return friendly;
        if (LOADER_MAPPINGS.TryGetValue(friendly, out var id)) return id;
        throw new ResourceNotFoundException($"{friendly} is not recognized as a loader id");
    }

    private ResourceKind GetResourceKind(string? kind)
    {
        if (kind == null) return ResourceKind.Modpack;
        return kind switch
        {
            "modpack" => ResourceKind.Modpack,
            "mod" => ResourceKind.Mod,
            "resourcepack" or "texturepack" or "texture" or "resource" => ResourceKind.ResourcePack,
            "shader" or "shaderpack" => ResourceKind.ShaderPack,
            _ => throw new ResourceNotFoundException($"{kind} is not recognized as a resource kind")
        };
    }
}