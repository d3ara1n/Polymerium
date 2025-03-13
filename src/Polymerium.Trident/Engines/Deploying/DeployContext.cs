using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Engines.Deploying;

public class DeployContext(string key, Profile.Rice setup, IServiceProvider provider, DeployOptions options)
{
    // 通过把 Context 填满，当内容被填满时代表部署完成

    internal DataLock? Artifact;
    internal DataLockBuilder? ArtifactBuilder;
    internal EntityManifest? Manifest;

    internal bool IsSolidified = false;
    internal bool IsLoaderProcess = false;
    internal bool IsPackageResolved = false;
    internal bool IsVanillaInstalled = false;
    public string Key => key;

    public Profile.Rice Setup => setup;

    // Preference
    public IServiceProvider Provider => provider;
    public DeployOptions Options => options;
}