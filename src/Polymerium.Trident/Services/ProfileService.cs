using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Tomlet;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Services;

public class ProfileService : IDisposable
{
    #region Injected Services

    private readonly PathService _pathService;

    #endregion

    private readonly IList<ProfileHandle> _profiles = new List<ProfileHandle>();

    public ProfileService(PathService pathService)
    {
        _pathService = pathService;

        var dir = pathService.InstanceDirectory;
        foreach (var ins in Directory.GetDirectories(dir)) Debug.WriteLine(ins);
    }

    public IReadOnlyList<Profile> Profiles => _profiles.Select(x => x.Value).ToImmutableList();

    public bool TryGetMutable(string key, [MaybeNullWhen(false)] out ProfileGuard profile)
    {
        var handle = _profiles.FirstOrDefault(x => x.Key == key);
        if (handle is not null)
        {
            profile = new ProfileGuard(this, handle);
            return true;
        }

        profile = default;
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

        profile = default;
        return false;
    }

    #region Handle & Guard sub classes

    internal class ProfileHandle(string key, Profile value, string path) : IAsyncDisposable
    {
        public string Key => key;
        public Profile Value => value;

        internal Task SaveAsync()
        {
            var toml = TomletMain.TomlStringFrom(value);
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return File.WriteAllTextAsync(path, toml);
        }

        public static ProfileHandle Create(string key, Profile value, string path)
        {
            return new ProfileHandle(key, value, path);
        }

        public static ProfileHandle Create(string key, string path)
        {
            if (File.Exists(path))
            {
                var profile = TomletMain.To<Profile>(File.ReadAllText(path));
                return new ProfileHandle(key, profile, path);
            }

            throw new FileNotFoundException("Profile not found");
        }

        #region Dispose

        private bool isDisposing;

        public async ValueTask DisposeAsync()
        {
            if (isDisposing) return;
            isDisposing = true;

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
            _root.OnProfileUpdated(Key);
        }
    }

    #endregion

    #region Profile Updated Event

    public class ProfileUpdatedEventArgs(string key, Profile profile) : EventArgs
    {
        public string Key => key;
        public Profile Value => profile;
    }

    public event EventHandler<ProfileUpdatedEventArgs> ProfileUpdated;

    internal void OnProfileUpdated(string key)
    {
        if (TryGetImmutable(key, out var value)) ProfileUpdated.Invoke(this, new ProfileUpdatedEventArgs(key, value));
    }

    #endregion

    #region Dispose

    private bool isDisposing;

    public void Dispose()
    {
        if (isDisposing) return;
        isDisposing = true;

        var tasks = _profiles.Select(x => x.DisposeAsync().AsTask()).ToArray();
        Task.WaitAll(tasks);

        _profiles.Clear();

        isDisposing = false;
    }

    #endregion
}