namespace Polymerium.Abstractions.ResourceResolving
{
    // poly-res://<type>:domain/...
    //   poly-res://mod/<id>/<version>
    //     poly-res://mod:modrinth/<id>/<version>
    //   poly-res://component/forge/<version>
    //   poly-res://core:mojang/<version>/assets/objects/<hash[..2]>/<hash>
    //   poly-res://core/<version>/assets/index
    //   poly-res://core/<version>/downloads/client
    // 只能获得元数据，没法用来下载文件，倒是可以替换 RestoreEngine 里部分指向 mojang 的链接
    public abstract class ResourceResolverBase
    {
    }
}