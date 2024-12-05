using Tomlet;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Services;

public class ProfileService : IDisposable
{
    internal class ProfileHandle(string key, Profile value, string path) : IAsyncDisposable
    {
        private bool isDisposing = false;

        public async ValueTask DisposeAsync()
        {
            if (isDisposing) return;
            isDisposing = true;

            await SaveAsync();
        }

        internal Task SaveAsync()
        {
            var toml = TomletMain.TomlStringFrom(value);
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return File.WriteAllTextAsync(path, toml);
        }

        public static ProfileHandle Create(string key, Profile value, string path) => new(key, value, path);

        public static ProfileHandle Create(string key, string path)
        {
            if (File.Exists(path))
            {
                var profile = TomletMain.To<Profile>(File.ReadAllText(path));
                return new ProfileHandle(key, profile, path);
            }
            else throw new FileNotFoundException("Profile not found");
        }
    }

    public class ProfileGuard : IAsyncDisposable
    {
        private readonly ProfileHandle _handle;

        internal ProfileGuard(ProfileHandle handle)
        {
            _handle = handle;
        }

        public async ValueTask DisposeAsync()
        {
            await _handle.SaveAsync();
        }
    }

    private readonly IList<ProfileHandle> _profiles = new List<ProfileHandle>();

    private bool isDisposing = false;

    public ProfileService()
    {
        
    }
    
    public void Dispose()
    {
        if (isDisposing) return;
        isDisposing = true;

        var tasks = _profiles.Select(x => x.DisposeAsync().AsTask()).ToArray();
        Task.WaitAll(tasks);

        _profiles.Clear();

        isDisposing = false;
    }
}