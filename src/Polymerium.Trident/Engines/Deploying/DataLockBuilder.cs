using IBuilder;
using Semver;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Engines.Deploying;

public class DataLockBuilder : IBuilder<DataLock>
{
    private readonly List<string> _gameArguments = [];
    private readonly List<string> _javaArguments = [];
    private readonly List<DataLock.Library> _libraries = [];
    private readonly List<DataLock.Parcel> _parcels = [];
    private DataLock.AssetData? _assetIndex;
    private uint? _javaMajorVersion;
    private string? _mainClass;
    private DataLock.ViabilityData? _viability;

    public IList<DataLock.Parcel> Parcels => _parcels;
    public IList<DataLock.Library> Libraries => _libraries;

    public DataLock Build()
    {
        ArgumentNullException.ThrowIfNull(_assetIndex);
        ArgumentNullException.ThrowIfNull(_javaMajorVersion);
        ArgumentNullException.ThrowIfNull(_mainClass);
        ArgumentNullException.ThrowIfNull(_viability);
        ArgumentNullException.ThrowIfNull(_gameArguments);
        ArgumentNullException.ThrowIfNull(_javaArguments);

        return new DataLock(_viability,
                            _mainClass,
                            _javaMajorVersion.Value,
                            _gameArguments,
                            _javaArguments,
                            _libraries,
                            _parcels,
                            _assetIndex);
    }

    public DataLockBuilder SetViability(DataLock.ViabilityData viability)
    {
        _viability = viability;
        return this;
    }

    public DataLockBuilder ClearGameArguments()
    {
        _gameArguments.Clear();
        return this;
    }

    public DataLockBuilder AddGameArgument(string arg)
    {
        arg = arg.Trim();
        _gameArguments.Add(arg);
        return this;
    }

    public DataLockBuilder AddJvmArgument(string arg)
    {
        arg = arg.Trim();
        _javaArguments.Add(arg);
        return this;
    }

    public DataLockBuilder AddParcel(DataLock.Parcel parcel)
    {
        _parcels.Add(parcel);
        return this;
    }

    public DataLockBuilder AddLibrary(DataLock.Library library)
    {
        // var found = _libraries.FirstOrDefault(x => x.Id.Namespace == library.Id.Namespace
        //                                         && x.Id.Name == library.Id.Name
        //                                         && x.Id.Platform == library.Id.Platform
        //                                         && x.Id.Extension == library.Id.Extension);
        // if (found != null)
        // {
        //     if (SemVersion.TryParse(found.Id.Version, out var oldVersion)
        //      && SemVersion.TryParse(library.Id.Version, out var newVersion)
        //      && newVersion.CompareSortOrderTo(oldVersion) > 0)
        //     {
        //         _libraries.Remove(found);
        //         _libraries.Add(library);
        //     }
        // }
        // else
        // {
        //     _libraries.Add(library);
        // }

        // 傻逼 Fabric 两个不同版本库会导致冲突，NeoForge 则必须多版本库同时存在
        if (_libraries.All(x => x != library))
            _libraries.Add(library);

        return this;
    }

    public DataLockBuilder SetAssetIndex(DataLock.AssetData index)
    {
        _assetIndex = index;
        return this;
    }

    public DataLockBuilder SetJavaMajorVersion(uint version)
    {
        _javaMajorVersion = version;
        return this;
    }

    public DataLockBuilder SetMainClass(string mainClass)
    {
        _mainClass = mainClass;
        return this;
    }
}