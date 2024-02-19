namespace Polymerium.Trident.Services.Instances
{
    public enum DeployStage
    {
        CheckArtifact,
        InstallVanilla,
        ResolveAttachments,
        ProcessLoaders,
        BuildArtifact,
        BuildTransient,
        SolidifyTransient
    }
}