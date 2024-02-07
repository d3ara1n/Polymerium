using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying;

public class TransientData
{
    public TransientData(Artifact source)
    {
        foreach (var parcel in source.Parcels)
            // 全部作为 Fragile 添加
            Files.Add(new TransientFile(parcel.SourcePath, parcel.TargetPath, parcel.Url, parcel.Sha1));
    }
    // 文件分为 Fragile 文件，具有 SourcePath TargetPath Url Sha1，下载到 SourcePath 并软连接到 TargetPath
    // Persistent 文件，具有 SourcePath TargetPath，复制 SourcePath 到 TargetPath
    // Present 文件，具有 SourcePath Url Sha1，下载到 SourcePath

    public IList<TransientFile> Files { get; } = new List<TransientFile>();
}