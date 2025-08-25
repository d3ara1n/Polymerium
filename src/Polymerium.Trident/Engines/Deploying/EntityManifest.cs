namespace Polymerium.Trident.Engines.Deploying
{
    public class EntityManifest
    {
        // 文件分为 Fragile 文件，具有 TargetPath Url Hash，下载到 Path 并软连接到 TargetPath
        // Persistent 文件，具有 Path TargetPath，复制 Path 到 TargetPath，IsPhantom 则只创建软连接
        // Present 文件，具有 Path Url Hash，下载到 Path
        // Explosive 文件，会解压到目标目录，IsDestructive 则会清空目录里的其他文件

        public IList<FragileFile> FragileFiles { get; } = new List<FragileFile>();
        public IList<PersistentFile> PersistentFiles { get; } = new List<PersistentFile>();
        public IList<PresentFile> PresentFiles { get; } = new List<PresentFile>();
        public IList<ExplosiveFile> ExplosiveFiles { get; } = new List<ExplosiveFile>();

        #region Nested type: ExplosiveFile

        public record ExplosiveFile(string SourcePath, string TargetDirectory, bool Unwrap = false);

        #endregion

        #region Nested type: FragileFile

        public record FragileFile(string SourcePath, string TargetPath, Uri Url, string? Hash);

        #endregion

        #region Nested type: PersistentFile

        public record PersistentFile(string SourcePath, string TargetPath, bool IsPhantom);

        #endregion

        #region Nested type: PresentFile

        public record PresentFile(string Path, Uri Url, string? Hash);

        #endregion
    }
}
