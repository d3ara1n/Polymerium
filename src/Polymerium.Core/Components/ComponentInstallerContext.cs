using System.Collections.Generic;
using System.Linq;
using IBuilder;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Abstractions.Models.Game;

namespace Polymerium.Core.Components;

public class ComponentInstallerContext : IBuilder<PolylockData>
{
    private readonly Dictionary<string, string> cargo = new();
    private readonly List<string> gameArguments = new();
    private readonly List<string> jvmArguments = new();
    private readonly List<Library> libraries = new();
    private readonly List<PolylockAttachment> attachments = new();

    public ComponentInstallerContext(GameInstance instance)
    {
        Instance = instance;
    }

    public string GetCoreVersion()
    {
        return Instance.Metadata.Components.Any(x => x.Identity == "net.minecraft")
            ? Instance.Metadata.Components.First(x => x.Identity == "net.minecraft").Version
            : null;
    }

    public GameInstance Instance { get; }
    public string MainClass { get; private set; }
    public int JavaMajorVersionRequired { get; private set; }
    public AssetIndex AssetIndex { get; private set; }
    public IDictionary<string, string> Cargo => cargo;
    public IEnumerable<string> GameArguments => gameArguments;
    public IEnumerable<string> JvmArguments => jvmArguments;
    public IEnumerable<Library> Libraries => libraries;
    public IEnumerable<PolylockAttachment> Attachments => attachments;

    public PolylockData Build()
    {
        return new PolylockData
        {
            MainClass = MainClass,
            Cargo = Cargo,
            Libraries = Libraries,
            AssetIndex = AssetIndex,
            Attachments = Attachments,
            GameArguments = GameArguments,
            JvmArguments = JvmArguments,
            JavaMajorVersionRequired = JavaMajorVersionRequired
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

    public void AppendGameArgument(string value)
    {
        gameArguments.Add(value);
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
        libraries.Add(library);
    }


    public void AddAttachment(PolylockAttachment attachment)
    {
        attachments.Add(attachment);
    }
}