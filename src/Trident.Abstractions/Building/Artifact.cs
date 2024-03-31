namespace Trident.Abstractions.Building
{
    public record Artifact(
        Artifact.ViabilityData Viability,
        string MainClass,
        uint JavaMajorVersion,
        Artifact.AssetData AssetIndex,
        IEnumerable<string> GameArguments,
        IEnumerable<string> JvmArguments,
        IEnumerable<Artifact.Library> Libraries,
        IEnumerable<Artifact.Parcel> Parcels,
        IEnumerable<Artifact.Processor> Processors)
    {
        public static ArtifactBuilder Builder()
        {
            return new ArtifactBuilder();
        }

        public bool Verify(string key, string watermark, string home)
        {
            return Viability.Key == key && Viability.Watermark == watermark && Viability.Home == home;
        }

        public record ViabilityData(string Key, string Watermark, string Home);

        // IsNative 决定是否解压到 Natives 目录，IsPresent 决定是否添加到 ClassPath，两者互不干扰
        public record Library(
            Library.Identity Id,
            Uri Url,
            string? Sha1,
            bool IsNative = false,
            bool IsPresent = true)
        {
            public record Identity(string Namespace, string Name, string Version, string? Platform, string Extension);
        }

        // 根据 Sha1 检查 SourcePath 文件有效性，无效则下载，无法下载就报错。
        public record Parcel(string SourcePath, string TargetPath, Uri Url, string? Sha1);

        // 每次构建时都会生成一份 keywords 列表用于判断条件，构建前后使用的 keywords 是同一份。
        // 关键字列表包含
        // 加载器列表 component:loader.trident.storage
        // 使用的账号类型 account_type:authlib-injector|online
        // 整合包来源 modpack_source:curseforge
        // 启动方式 launch_type:fire-and-forget
        // 
        // Condition 形如 !a|b&c
        // 先按 | 分割对结果取 any，对细化结果按 & 分割，按是否取反取 all
        //
        // Artifact + Processors -> TransientSolidifyingData 用于部署的最后阶段即固化
        public record Processor(string? Condition, string Action, string? Data);

        public record AssetData(string Id, Uri Url, string Sha1);
    }
}