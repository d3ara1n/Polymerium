using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Models.Modrinth;

namespace Polymerium.Core.Importers;

public class ModrinthImporter : ImporterBase
{
    public override async Task<Result<GameInstance, string>> ProcessAsync(Stream stream)
    {
        var archive = new ZipArchive(stream);
        var indexFile = archive.GetEntry("modrinth.index.json");
        if (indexFile != null)
        {
            using var reader = new StreamReader(indexFile.Open());
            var index = JsonConvert.DeserializeObject<ModrinthModpackIndex>(await reader.ReadToEndAsync());
            if (index.Game != "minecraft") return Failed($"{index.Game} is not MINECRAFT");
            // [{client/server}-]overrides 导出到 local repository。这些内容是一次性，是不是应该。。。
            // 不应该，直接用 InstanceManager 往目录写就完事了
            // 那就打个待办吧，毕竟 InstanceManager 还没写呢。
            // 不需要，直接灌入 poly-file://{id}/ 就行
            // 但是此时 GameInstance 还没加入 InstanceManager 的托管范围，怎么获得目录呢（囧
            // TODO: 还是得打个待办
            var instance = new GameInstance(new GameMetadata(), index.VersionId, new FileBasedLaunchConfiguration(),
                index.Name, index.Name);
            // 由于是本地导入的，所以没有 ReferenceSource，也不上锁
            // 通过下载中心导入的包会是 poly-res://modpack:modrinth/<id|slug>
            foreach (var dependency in index.Dependencies)
                instance.Metadata.Components.Add(new Component
                {
                    Version = dependency.Version,
                    Identity = dependency.Id switch
                    {
                        "minecraft" => "net.minecraft",
                        "forge" => "net.minecraftforge",
                        "fabric-loader" => "net.fabricmc.fabric-loader",
                        "quilt-loader" => "net.quiltmc.quilt-loader",
                        _ => dependency.Id
                    }
                });

            foreach (var file in index.Files.Where(x => x.Envs.HasValue &&
                                                        (x.Envs.Value.Client == ModrinthModpackEnv.Optional ||
                                                         x.Envs.Value.Client == ModrinthModpackEnv.Required)))
            {
                // TODO: poly-res://file:remote/{path}?sha1={sha1}&source={another-url}
            }

            return Finished(instance);
        }

        return Failed("Pack file is not valid modrinth modpack");
    }
}