namespace Polymerium.Avalonia.Models;

// NOTE: 集合的就地分组展示模型——Name 供 UI 显示，Uri 是写入 Entry.Source 的规范身份串。
//  已存在集合的 Uri 取自 profile 原样（不重编码），新建集合的 Uri 由 CollectionHelper 编码一次。
public sealed record CollectionModel(string Name, string Uri);
