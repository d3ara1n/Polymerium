namespace Polymerium.Abstractions.ResourceResolving;

// poly-res://<type>:domain/...
//   poly-res://mod/<id>/<version>
//     poly-res://mod:modrinth/<id>/<version>
//   poly-res://modpack/<id>/<version>
//   poly-res://file/<local_path_in_instance_dir>
//     poly-res://file:remove/<local_path_in_instance_dir>?sha1=<sha1>&source=<url>
//     poly-res://file:local/<local_path_in_instance_dir> 由于 local repository 自带 sha1，这里就不需要记录了
// 得有个基于文件的缓存避免 Resolving 总是发生网络IO
// 不需要文件缓存，PolylockData 就是文件缓存，PolylockData.Attachments 就包含了 Resolving 的结果
public abstract class ResourceResolverBase
{
    public Result<ResolveResult, ResultError> Ok(string url, string hash)
    {
        return Result<ResolveResult, ResultError>.Ok(
            new ResolveResult
            {
                Url = url,
                Hash = hash
            });
    }
}