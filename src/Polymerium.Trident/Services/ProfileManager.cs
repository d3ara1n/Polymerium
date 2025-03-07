using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Services;

// 只有 profile.json 是共享文件，其他都是独占文件，因此不对其他文件做 Guard 保护。
public class ProfileManager : IDisposable
{
    #region Injected

    private readonly ILogger<ProfileManager> _logger;

    #endregion

    private readonly IList<ProfileHandle> _profiles = new List<ProfileHandle>();
    private readonly JsonSerializerOptions _serializerOptions;
    internal readonly IList<ReservedKey> ReservedKeys = new List<ReservedKey>();

    public ProfileManager(ILogger<ProfileManager> logger)
    {
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        var dir = new DirectoryInfo(PathDef.Default.InstanceDirectory);
        if (!dir.Exists)
            return;

        foreach (var ins in dir.GetDirectories())
        {
            var path = PathDef.Default.FileOfProfile(ins.Name);
            if (!File.Exists(path))
                continue;

            try
            {
                var bomb = PathDef.Default.FileOfBomb(ins.Name);
                if (File.Exists(bomb))
                {
                    ins.Delete(true);
                    continue;
                }

                var handle = ProfileHandle.Create(ins.Name, path, _serializerOptions);
                _profiles.Add(handle);
                logger.LogInformation("{} scanned", handle.Key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add profile {}", ins.Name);
            }
        }
    }

    public IEnumerable<(string, Profile)> Profiles => _profiles.Select(x => (x.Key, x.Value));

    public bool TryGetMutable(string key, [MaybeNullWhen(false)] out ProfileGuard profile)
    {
        var handle = _profiles.FirstOrDefault(x => x.Key == key);
        if (handle is not null)
        {
            profile = new ProfileGuard(this, handle);
            return true;
        }

        profile = null;
        return false;
    }

    public bool TryGetImmutable(string key, [MaybeNullWhen(false)] out Profile profile)
    {
        var handle = _profiles.FirstOrDefault(x => x.Key == key);
        if (handle is not null)
        {
            profile = handle.Value;
            return true;
        }

        profile = null;
        return false;
    }

    public ReservedKey RequestKey(string key)
    {
        var sanitized = !string.IsNullOrEmpty(key)
                            ? string
                             .Join(string.Empty,
                                   key.Trim().ToLower().Where(x => !Path.GetInvalidFileNameChars().Contains(x)))
                             .Replace(' ', '_')
                            : "_";
        while (_profiles.Any(x => x.Key == sanitized) || ReservedKeys.Any(x => x.Key == sanitized))
            sanitized += '_';

        var rv = new ReservedKey(sanitized, this);
        ReservedKeys.Add(rv);
        return rv;
    }

    public void Add(ReservedKey key, Profile profile)
    {
        var handle = new ProfileHandle(key.Key, profile, PathDef.Default.FileOfProfile(key.Key), _serializerOptions);
        handle.SaveAsync().Wait();
        _profiles.Add(handle);
        key.Dispose();
        handle.SaveAsync().Wait();

        _logger.LogInformation("{} added", handle.Key);
        OnProfileAdded(key.Key, profile);
    }

    public void Remove(string key)
    {
        var handle = _profiles.FirstOrDefault(x => x.Key == key);
        if (handle is null)
            throw new InvalidOperationException($"{key} is not in profiles");
        _profiles.Remove(handle);

        _logger.LogInformation("{} removed", key);
        OnProfileRemoved(key, handle.Value);
    }

    public void Update(
        string key,
        string? source,
        string name,
        string version,
        string? loader,
        IReadOnlyList<string> packages,
        IDictionary<string, object> overrides)
    {
        var handle = _profiles.FirstOrDefault(x => x.Key == key);
        if (handle is null)
            throw new InvalidOperationException($"{key} is not in profiles");

        handle.Value.Name = name;
        handle.Value.Setup.Source = source;
        handle.Value.Setup.Version = version;
        handle.Value.Setup.Loader = loader;
        handle.Value.Setup.Stage.Clear();
        foreach (var package in packages)
            handle.Value.Setup.Stage.Add(package);

        foreach (var (k, v) in overrides)
            handle.Value.Overrides[k] = v;

        handle.SaveAsync().Wait();
        _logger.LogInformation("{} updated", key);
        OnProfileUpdated(key, handle.Value);
    }

    #region Profile Changed Event

    public class ProfileChangedEventArgs(string key, Profile profile) : EventArgs
    {
        public string Key => key;
        public Profile Value => profile;
    }

    public event EventHandler<ProfileChangedEventArgs>? ProfileUpdated;

    public event EventHandler<ProfileChangedEventArgs>? ProfileRemoved;

    public event EventHandler<ProfileChangedEventArgs>? ProfileAdded;

    internal void OnProfileUpdated(string key, Profile profile) =>
        ProfileUpdated?.Invoke(this, new ProfileChangedEventArgs(key, profile));

    internal void OnProfileRemoved(string key, Profile profile) =>
        ProfileRemoved?.Invoke(this, new ProfileChangedEventArgs(key, profile));

    internal void OnProfileAdded(string key, Profile profile) =>
        ProfileAdded?.Invoke(this, new ProfileChangedEventArgs(key, profile));

    #endregion

    #region Dispose

    private bool _isDisposing;

    public void Dispose()
    {
        if (_isDisposing)
            return;

        _isDisposing = true;

        var tasks = _profiles.Select(x => x.DisposeAsync().AsTask()).ToArray();
        Task.WaitAll(tasks);

        _profiles.Clear();

        _isDisposing = false;
    }

    #endregion
}