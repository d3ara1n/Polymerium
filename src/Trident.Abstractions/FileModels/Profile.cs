namespace Trident.Abstractions.FileModels;

public class Profile(string name, Profile.Rice setup, IDictionary<string, object>? overrides)
{
    public const string OVERRIDE_JAVA_HOME = "java.home";
    public const string OVERRIDE_JAVA_MAX_MEMORY = "java.max_memory";
    public const string OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS = "java.additional_arguments";
    public const string OVERRIDE_WINDOW_HEIGHT = "window.height";
    public const string OVERRIDE_WINDOW_WIDTH = "window.width";
    public const string OVERRIDE_WINDOW_TITLE = "window.title";
    public const string OVERRIDE_BEHAVIOR_DEPLOY_METHOD = "behavior.deploy.method";
    public const string OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE = "behavior.deploy.fastmode";
    public const string OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY = "behavior.resolve.dependency";

    public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));
    public Rice Setup { get; private set; } = setup ?? throw new ArgumentNullException(nameof(setup));

    public IDictionary<string, object> Overrides { get; private set; } = overrides ?? new Dictionary<string, object>();

    #region Nested type: Rice

    public class Rice(string? source, string version, string? loader, IList<Rice.Entry>? packages)
    {
        public string? Source { get; set; } = source;
        public string Version { get; set; } = version ?? throw new ArgumentNullException(nameof(version));
        public string? Loader { get; set; } = loader;
        public IList<Entry> Packages { get; private set; } = packages ?? new List<Entry>();

        #region Nested type: Entry

        public class Entry(string purl, bool enabled, string? source, IList<string>? tags)
        {
            public string Purl { get; set; } = purl ?? throw new ArgumentNullException(nameof(purl));
            public bool Enabled { get; set; } = enabled;
            public string? Source { get; set; } = source;
            public IList<string> Tags { get; private set; } = tags ?? new List<string>();
        }

        #endregion
    }

    #endregion
}