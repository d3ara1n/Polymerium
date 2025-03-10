using System.Text.Json;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Services.Profiles;

internal class ProfileHandle(string key, Profile value, string path, JsonSerializerOptions options) : IAsyncDisposable
{
    public string Key => key;
    public Profile Value => value;

    internal bool IsActive { get; set; } = true;

    internal Task SaveAsync()
    {
        if (!IsActive)
            return Task.CompletedTask;
        var json = JsonSerializer.Serialize(Value, options);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return File.WriteAllTextAsync(path, json);
    }

    public static ProfileHandle Create(string key, Profile value, string path, JsonSerializerOptions options) =>
        new(key, value, path, options);

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
        if (_isDisposing)
            return;

        _isDisposing = true;

        await SaveAsync();
    }

    #endregion
}