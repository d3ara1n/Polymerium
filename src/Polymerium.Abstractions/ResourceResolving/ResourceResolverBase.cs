using System;

namespace Polymerium.Abstractions.ResourceResolving;

// poly-res://<domain>@<type>/...
//   poly-res://mod/<id>/<version>
//     poly-res://modrinth@mod/<id>/<version>
//   poly-res://modpack/<id>/<version>
//   poly-res://file/<local_path_in_instance_dir>
//     poly-res://remote@file/<local_path_in_instance_dir>?sha1=<sha1>&source=<url>
//     poly-res://local@file/<local_path_in_instance_dir> 由于 local repository 自带 sha1，这里就不需要记录了
// 得有个基于文件的缓存避免 Resolving 总是发生网络IO
// 不需要文件缓存，PolylockData 就是文件缓存，PolylockData.Attachments 就包含了 Resolving 的结果

// domain 的解析优先级：特定 domain 的 url 会被带有特定 domain name 的 resolver 优先处；
// 不带 domain name 的 resolver 会在最后接受任意 domain 的url；
// 不带 domain 的 url 只能被不带 domain name 的 resolver 处理
public abstract class ResourceResolverBase
{
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public ResolverContext Context { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

    public Result<ResolveResult, ResolveResultError> Ok(Uri path, Uri source, string? hash)
    {
        return Result<ResolveResult, ResolveResultError>.Ok(new ResolveResult(source, path, hash));
    }
}