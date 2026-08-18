using System.Windows.Input;
using FluentIcons.Common;

namespace Polymerium.Avalonia.Models;

// NOTE: ExplorerActionModel 的视图投影——LangKey/Icon 原样携带，Handler 换成包装了
//  「成功即清空待定区」语义的命令，供提交区的 DataTemplate 直接绑定。
public sealed record ExplorerActionItemModel(string LangKey, Symbol Icon, ICommand Command);
