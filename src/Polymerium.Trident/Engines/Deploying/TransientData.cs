namespace Polymerium.Trident.Engines.Deploying;

public class TransientData
{
    public const string PROCESSOR_TRIDENT_STORAGE = "processor.trident.storage";

    // 文件分为 Fragile 文件，具有 SourcePath TargetPath Url Sha1，下载到 SourcePath 并软连接到 TargetPath
    // Persistent 文件，具有 SourcePath TargetPath，复制 SourcePath 到 TargetPath
    // Present 文件，具有 SourcePath Url Sha1，下载到 SourcePath

    public IList<FragileFile> FragileFiles { get; } = new List<FragileFile>();
    public IList<PersistentFile> PersistentFiles { get; } = new List<PersistentFile>();
    public IList<PresentFile> PresentFiles { get; } = new List<PresentFile>();
    public IList<ExplosiveFile> ExplosiveFiles { get; } = new List<ExplosiveFile>();

    public void AddFragile(FragileFile fragile)
    {
        FragileFiles.Add(fragile);
    }

    public void AddPersistent(PersistentFile persistent)
    {
        PersistentFiles.Add(persistent);
    }

    public void AddPresent(PresentFile present)
    {
        PresentFiles.Add(present);
    }

    public void AddExplosive(ExplosiveFile explosive)
    {
        ExplosiveFiles.Add(explosive);
    }

    public record FragileFile(string SourcePath, string TargetPath, Uri Url, string? Sha1);

    public record PersistentFile(string SourcePath, string TargetPath);

    public record PresentFile(string SourcePath, Uri Url, string? Sha1);

    public record ExplosiveFile(string SourcePath, string TargetDirectory);
}