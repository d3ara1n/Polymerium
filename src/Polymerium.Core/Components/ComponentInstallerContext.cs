using System.Collections.Generic;
using IBuilder;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Abstractions.Models.Game;

namespace Polymerium.Core.Components;

public class ComponentInstallerContext : IBuilder<PolylockData>
{
    private readonly List<PolylockAttachment> attachments = new();
    private readonly Dictionary<string, string> cargo = new();
    private readonly List<string> gameArguments = new();
    private readonly List<string> jvmArguments = new();
    private readonly List<Library> libraries = new();

    public ComponentInstallerContext(GameInstance instance)
    {
        Instance = instance;
    }

    public GameInstance Instance { get; }
    public string? MainClass { get; private set; }
    public int? JavaMajorVersionRequired { get; private set; }
    public AssetIndex? AssetIndex { get; private set; }
    public IDictionary<string, string> Cargo => cargo;
    public IEnumerable<string> GameArguments => gameArguments;
    public IEnumerable<string> JvmArguments => jvmArguments;
    public IEnumerable<Library> Libraries => libraries;
    public IEnumerable<PolylockAttachment> Attachments => attachments;

    public PolylockData Build()
    {
        return new PolylockData
        {
            MainClass = MainClass ?? string.Empty,
            Cargo = Cargo,
            Libraries = Libraries,
            AssetIndex = AssetIndex ?? default,
            Attachments = Attachments,
            GameArguments = GameArguments,
            JvmArguments = JvmArguments,
            JavaMajorVersionRequired = JavaMajorVersionRequired ?? 0
        };
    }

    public void SetMainClass(string mainClass)
    {
        MainClass = mainClass;
    }

    public void SetAssetIndex(AssetIndex index)
    {
        AssetIndex = index;
    }

    public void SetJavaVersion(int major)
    {
        JavaMajorVersionRequired = major;
    }

    public void OverrideGameArguments()
    {
        gameArguments.Clear();
    }

    public void AppendGameArgument(string value)
    {
        if (!gameArguments.Contains(value)) gameArguments.Add(value);
    }

    public void AppendJvmArguments(string value)
    {
        jvmArguments.Add(value);
    }

    public void AddCrate(string key, string value)
    {
        if (cargo.ContainsKey(key))
            cargo[key] = value;
        else
            cargo.Add(key, value);
    }

    public void AddLibrary(Library library)
    {
        var found = false;
        foreach (var exist in libraries)
            //var existTuple = exist.Name.Split(':');
            //var tuple = library.Name.Split(':');
            //if (existTuple.Take(2).Concat(existTuple.Skip(3)).SequenceEqual(tuple.Take(2).Concat(tuple.Skip(3))) &&
            //    exist.IsNative == library.IsNative &&
            //    exist.PresentInClassPath == library.PresentInClassPath)
            //{
            //    exist.Name = library.Name;
            //    exist.Path = library.Path;
            //    exist.Sha1 = library.Sha1;
            //    exist.Url = library.Url;
            //    found = true;
            //    break;
            //}
            if (exist.Name == library.Name && exist.IsNative == library.IsNative &&
                exist.PresentInClassPath == library.PresentInClassPath)
                found = true;

        if (!found) libraries.Add(library);
    }

    public void AddAttachment(PolylockAttachment attachment)
    {
        attachments.Add(attachment);
    }
}