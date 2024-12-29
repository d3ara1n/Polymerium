using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Services;

// 只有 profile.json 是共享文件，其他都是独占文件，因此不对其他文件做 Guard 保护。
public class ProfileService : IDisposable
{
    #region Injected Services

    #endregion

    private readonly IList<ProfileHandle> _profiles = new List<ProfileHandle>();
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IList<ReservedKey> _reservedKeys = new List<ReservedKey>();

    public ProfileService()
    {
        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        var dir = new DirectoryInfo(PathDef.Default.InstanceDirectory);
        foreach (var ins in dir.GetDirectories())
        {
            var path = PathDef.Default.FileOfProfile(ins.Name);
            if (File.Exists(path))
                try
                {
                    var handle = ProfileHandle.Create(ins.Name, path, _serializerOptions);
                    _profiles.Add(handle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
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
        var sanitized = string.Join(string.Empty, key.Where(x => !Path.GetInvalidFileNameChars().Contains(x)));
        while (_profiles.Any(x => x.Key == sanitized) || _reservedKeys.Any(x => x.Key == sanitized)) sanitized += '_';
        var rv = new ReservedKey(sanitized, this);
        _reservedKeys.Add(rv);
        return rv;
    }

    #region Key requseted sub classes

    public class ReservedKey : IDisposable
    {
        private readonly ProfileService _root;

        internal ReservedKey(string key, ProfileService root)
        {
            _root = root;
            Key = key;
        }

        public string Key { get; }

        public void Dispose()
        {
            // 可能会遇到临界问题，但概率很低
            _root._reservedKeys.Remove(this);
        }
    }

    #endregion

    #region Handle & Guard sub classes

    internal class ProfileHandle(string key, Profile value, string path, JsonSerializerOptions options)
        : IAsyncDisposable
    {
        public string Key => key;
        public Profile Value => value;

        internal Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(Value, options);
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return File.WriteAllTextAsync(path, json);
        }

        public static ProfileHandle Create(string key, Profile value, string path, JsonSerializerOptions options)
        {
            return new ProfileHandle(key, value, path, options);
        }

        public static ProfileHandle Create(string key, string path, JsonSerializerOptions options)
        {
            if (File.Exists(path))
            {
                var profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(path), options)!;
                return new ProfileHandle(key, profile, path, options);
            }

            throw new FileNotFoundException("Profile not found");
        }

        #region Dispose

        private bool _isDisposing;

        public async ValueTask DisposeAsync()
        {
            if (_isDisposing) return;
            _isDisposing = true;

            await SaveAsync();
        }

        #endregion
    }

    public class ProfileGuard : IAsyncDisposable
    {
        private readonly ProfileHandle _handle;
        private readonly ProfileService _root;

        internal ProfileGuard(ProfileService root, ProfileHandle handle)
        {
            _root = root;
            _handle = handle;
        }

        public string Key => _handle.Key;
        public Profile Value => _handle.Value;

        public async ValueTask DisposeAsync()
        {
            await _handle.SaveAsync();
            _root.OnProfileUpdated(Key, _handle.Value);
        }
    }

    #endregion

    #region Profile Changed Event

    public class ProfileChangedEventArgs(string key, Profile profile) : EventArgs
    {
        public string Key => key;
        public Profile Value => profile;
    }

    public event EventHandler<ProfileChangedEventArgs>? ProfileUpdated;

    public event EventHandler<ProfileChangedEventArgs>? ProfileRemoved;

    public event EventHandler<ProfileChangedEventArgs>? ProfileAdded;

    private void OnProfileUpdated(string key, Profile profile)
    {
        ProfileUpdated?.Invoke(this, new ProfileChangedEventArgs(key, profile));
    }

    private void OnProfileRemoved(string key, Profile profile)
    {
        ProfileRemoved?.Invoke(this, new ProfileChangedEventArgs(key, profile));
    }

    private void OnProfileAdded(string key, Profile profile)
    {
        ProfileAdded?.Invoke(this, new ProfileChangedEventArgs(key, profile));
    }

    #endregion

    #region Dispose

    private bool _isDisposing;

    public void Dispose()
    {
        if (_isDisposing) return;
        _isDisposing = true;

        var tasks = _profiles.Select(x => x.DisposeAsync().AsTask()).ToArray();
        Task.WaitAll(tasks);

        _profiles.Clear();

        _isDisposing = false;
    }

    #endregion
}