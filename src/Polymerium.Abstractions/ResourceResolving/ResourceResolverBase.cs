namespace Polymerium.Abstractions.ResourceResolving;

// poly-res://<type>:domain/...
//   poly-res://mod/<id>/<version>
//     poly-res://mod:modrinth/<id>/<version>
//   poly-res://modpack/<id>/<version>
// 只能获得元数据，没法用来下载文件，倒是可以替换 RestoreEngine 里部分指向 mojang 的链接
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