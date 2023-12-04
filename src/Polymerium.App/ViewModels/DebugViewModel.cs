using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNext;
using DotNext.Text.Json;
using Polymerium.Trident;
using Polymerium.Trident.Resolving;
using ReactiveUI;
using Tomlyn;

namespace Polymerium.App.ViewModels;

public class DebugViewModel(IEngine<ResolveInput, ResolveOutput> resolve) : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> DebugCommand { get; } = ReactiveCommand.Create(Debug);

    public static void Debug()
    {
        var profile = new Profile("1.20.1", "Me", "Summary", new Uri("http://www.example.com"),
            null, new Profile.MetaData("1.20.1", new List<Profile.MetaData.Loader>()
            {
                new(Profile.MetaData.Loader.COMPONENT_FORGE, "43.10.0")
            }, new List<Profile.MetaData.Layer>()
            {
                new(true, "builtin", null, new List<string>()
                {
                    "pkg:curseforge/114/514"
                })
            }), new List<Profile.TimelinePoint>()
            {
                new ("pkg:curseforge/1919/810", Profile.TimelinePoint.ActionKind.Create,
                    DateTimeOffset.Now, DateTimeOffset.Now, true)
            });
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var content = JsonSerializer.Serialize(profile, options);
        Console.WriteLine(content);
        var de = JsonSerializer.Deserialize<Profile>(content, options);
        Console.WriteLine(de);
    }
}