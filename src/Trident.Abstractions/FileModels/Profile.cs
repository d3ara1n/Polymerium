namespace Trident.Abstractions.FileModels;

public class Profile(string name, Profile.Rice setup, IDictionary<string, object>? overrides)
{
    public const string OVERRIDE_JAVA_HOME = "java.home";
    public const string OVERRIDE_JAVA_MAX_MEMORY = "java.max_memory";
    public const string OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS = "java.additional_arguments";
    public const string OVERRIDE_WINDOW_HEIGHT = "window.height";
    public const string OVERRIDE_WINDOW_WIDTH = "window.width";
    public const string OVERRIDE_WINDOW_TITLE = "window.title";

    public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));
    public Rice Setup { get; private set; } = setup ?? throw new ArgumentNullException(nameof(setup));

    public IDictionary<string, object> Overrides { get; private set; } = overrides ?? new Dictionary<string, object>();

    #region Nested type: Rice

    public class Rice(
        string? source,
        string version,
        string? loader,
        IList<string>? stage,
        IList<string>? stash,
        IList<string>? draft)
    {
        public string? Source { get; set; } = source;
        public string Version { get; set; } = version ?? throw new ArgumentNullException(nameof(version));
        public string? Loader { get; set; } = loader;
        public IList<string> Stage { get; private set; } = stage ?? new List<string>();
        public IList<string> Stash { get; private set; } = stash ?? new List<string>();
        public IList<string> Draft { get; private set; } = draft ?? new List<string>();
    }

    #endregion
}