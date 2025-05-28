using IBuilder;
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

    #region IBuilder<DataLock> Members

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

    #endregion

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
        if (!_gameArguments.Contains(arg))
            _gameArguments.Add(arg);
        return this;
    }

    public DataLockBuilder AddJvmArgument(string arg)
    {
        arg = arg.Trim();
        if (!_javaArguments.Contains(arg))
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
        // 规则：
        //  允许除 IsNative 不同的同时存在，但不允许除了 IsPresent 不同的同时存在， IsPresent==True的优先
        var found = _libraries.FirstOrDefault(x => x.Id.Namespace == library.Id.Namespace
                                                && x.Id.Name == library.Id.Name
                                                && x.Id.Platform == library.Id.Platform
                                                && x.Id.Extension == library.Id.Extension
                                                && x.IsNative == library.IsNative);
        if (found != null)
        {
            // Present 只能有一个

            if (found.Id.Version == library.Id.Version)
            {
                if (library.IsPresent)
                    // 保留新的
                    _libraries.Remove(found);
                else
                    // 保留旧的
                    return this;
            }
            else if (found.IsPresent && library.IsPresent)
            {
                _libraries.Remove(found);
            }
        }

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