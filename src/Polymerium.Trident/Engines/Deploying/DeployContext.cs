using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Engines.Deploying
{
    public class DeployContext(
        string key,
        Profile.Rice setup,
        IServiceProvider provider,
        DeployEngineOptions options,
        string verificationWatermark,
        JavaHomeLocatorDelegate javaHomeLocator)
    {
        // 通过把 Context 填满，当内容被填满时代表部署完成

        internal DataLock? Artifact;
        internal DataLockBuilder? ArtifactBuilder;
        internal bool IsLoaderProcessed = false;
        internal bool IsPackageResolved = false;
        internal bool IsRuntimeEnsured = false;
        internal bool IsSolidified = false;
        internal bool IsVanillaInstalled = false;
        internal EntityManifest? Manifest;
        internal BundledRuntime? Runtime;
        public string Key => key;

        public Profile.Rice Setup => setup;
        public IServiceProvider Provider => provider;
        public DeployEngineOptions Options => options;
        public string VerificationWatermark => verificationWatermark;
        public JavaHomeLocatorDelegate JavaHomeLocator => javaHomeLocator;
    }
}
