using Polymerium.Trident.Data;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System.ComponentModel;
using System.Text.Json;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Resources;
using Profile = Trident.Abstractions.Profile;

namespace Polymerium.Cli.Commands
{
    [Description("Display all the information about the instance")]
    public class InspectCommand(TridentContext trident, JsonSerializerOptions options)
        : Command<InspectCommand.Arguments>
    {
        public class Arguments : CommandSettings
        {
            [CommandArgument(0, "<key>")] public string Key { get; init; } = null!;
        }

        public override int Execute(CommandContext context, Arguments settings)
        {
            var path = trident.InstanceProfilePath(settings.Key);
            var handle = Handle<Profile>.Create(path, options);
            if (handle == null)
                throw new BadFormatException("The key is referred to a bad profile file");

            handle.Activated = false;
            var profile = handle.Value;
            var basis = new Dictionary<string, string>() { { "Name", profile.Name } };
            if (profile.Reference != null)
                basis.Add("Reference", profile.Reference.ToPurl());
            AnsiConsole.Write(new Rule($"[bold yellow]{Markup.Escape(settings.Key)}[/]"));
            foreach (var (k, v) in basis)
                AnsiConsole.MarkupLine("[bold aqua]{0}:[/] {1}", k, v);
            AnsiConsole.Write(new Rule("[bold yellow]Metadata[/]"));
            AnsiConsole.MarkupLine("[bold aqua]Version:[/] {0}", profile.Metadata.Version);
            AnsiConsole.MarkupLine("[bold aqua]Count of Layers:[/] {0}", profile.Metadata.Layers.Count);
            var layerN = 0;
            foreach (var layer in profile.Metadata.Layers)
            {
                var tree = new Tree($"[deepskyblue1]Layer #{layerN}[/]");
                tree.AddNode($"[aqua]Enabled:[/] {(layer.Enabled ? Emoji.Known.CheckMark : Emoji.Known.CrossMark)}");
                if (layer.Source != null && layer.Source == profile.Reference)
                    tree.AddNode(
                        $"[aqua]Locked by:[/] {layer.Source.ToPurl()}");
                var loaders = tree.AddNode($"[aqua]Loaders[/][gray]({layer.Loaders.Count})[/]");
                if (layer.Loaders.Any())
                    loaders.AddNode(new Rows(layer.Loaders.Select(x =>
                        new Markup($"[bold blue]{Markup.Escape(x.Identity)}[/]:[gray]{Markup.Escape(x.Version)}[/]"))));
                else
                    loaders.AddNode(new Markup("[gray]EMPTY[/]"));
                var attachments = tree.AddNode($"[aqua]Attachments[/][gray]({layer.Attachments.Count})[/]");
                if (layer.Attachments.Any())
                {
                    attachments.AddNode(new Rows(layer.Attachments.Take(10).Select<Attachment, IRenderable>(x =>
                        new Text(x.ToPurl())).Concat(layer.Attachments.Count > 10
                        ? new[] { new Markup($"[gray]...{layer.Attachments.Count - 10}[/]") }
                        : Enumerable.Empty<IRenderable>())));
                    var groups = layer.Attachments.GroupBy(x => x.Label switch
                    {
                        RepositoryLabels.CURSEFORGE => RepositoryLabels.CURSEFORGE,
                        RepositoryLabels.MODRINTH => RepositoryLabels.MODRINTH,
                        _ => "other"
                    }).ToArray();
                    var chart = new BreakdownChart()
                        .AddItems(groups, group => new BreakdownChartItem(group.Key,
                            group.Count(), group.Key switch
                            {
                                RepositoryLabels.CURSEFORGE => Color.DarkOrange,
                                RepositoryLabels.MODRINTH => Color.Green,
                                _ => Color.Blue
                            }));
                    attachments.AddNode(chart);
                }
                else
                    attachments.AddNode(new Markup("[gray]EMPTY[/]"));

                AnsiConsole.Write(tree);
                layerN++;
            }

            AnsiConsole.Write(new Rule("[yellow]Timeline[/]"));
            foreach (var activity in profile.Records.Timeline)
            {
                var format = "yyyy/MM/dd HH:mm:ss";
                var totalTime = profile.Records.ExtractTimeSpan(Profile.RecordData.TimelinePoint.TimelimeAction.Play) +
                                profile.Records.ExtractTimeSpan(Profile.RecordData.TimelinePoint.TimelimeAction.Deploy);
                var width = AnsiConsole.Console.Profile.Width;
                if (width - 43 >= 0) width -= 43;
                else width = 0;


                switch (activity.Action)
                {
                    case Profile.RecordData.TimelinePoint.TimelimeAction.Create:
                        AnsiConsole.MarkupLine(
                            $"[gray]{Markup.Escape(activity.BeginTime.ToString(format))}[/] [yellow]Create[/] {(activity.Source != null ? Markup.Escape(activity.Source.ToPurl()) : "[gray]NO SOURCE[/]")}");
                        break;
                    case Profile.RecordData.TimelinePoint.TimelimeAction.Update:
                        AnsiConsole.MarkupLine(
                            $"[gray]{Markup.Escape(activity.BeginTime.ToString(format))}[/] [fuchsia]Update[/] {(activity.Source != null ? Markup.Escape(activity.Source.ToPurl()) : "[gray]NO SOURCE[/]")}");
                        break;
                    case Profile.RecordData.TimelinePoint.TimelimeAction.Deploy:
                        {
                            var time = activity.EndTime - activity.BeginTime;
                            var percent = time / totalTime;
                            var count = (int)Math.Floor(percent * width);
                            AnsiConsole.MarkupLine(
                                $"[gray]{Markup.Escape(activity.BeginTime.ToString(format))}[/] [blue]Deploy {new string('\u2588', count)}[/] [gray]{time:g}[/]");
                        }
                        break;
                    case Profile.RecordData.TimelinePoint.TimelimeAction.Play:
                        {
                            var time = activity.EndTime - activity.BeginTime;
                            var percent = time / totalTime;
                            var count = (int)Math.Floor(percent * width);
                            AnsiConsole.MarkupLine(
                                $"[gray]{Markup.Escape(activity.BeginTime.ToString(format))}[/] [red]  Play {new string('\u2588', count)}[/] [gray]{time:g}[/]");
                        }
                        break;
                }
            }


            AnsiConsole.Write(new Rule("[yellow]Note[/]"));
            if (!string.IsNullOrWhiteSpace(profile.Records.Note))
                AnsiConsole.Write(new Text(profile.Records.Note));
            else
                AnsiConsole.Write(new Rule("[gray]EMPTY[/]").NoBorder());
            return 0;
        }

        public override ValidationResult Validate(CommandContext context, Arguments settings)
        {
            if (string.IsNullOrEmpty(settings.Key) || !File.Exists(trident.InstanceProfilePath(settings.Key)))
                return ValidationResult.Error("Profile with the specific key does not exist");
            return base.Validate(context, settings);
        }
    }
}