using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentIcons.Common;

namespace Polymerium.Avalonia.Models;

// NOTE: session 向 explorer 宿主自声明的提交动作——宿主只按 LangKey/Icon/命令泛型渲染，不认识
//  任何具体动作。Handler 收到的是待定区快照，返回 false 表示未落盘、待定区应保留；CanExecute
//  为 null 表示默认条件（待定区非空）即可，非 null 时由宿主再叠加该条件。
public sealed record ExplorerActionModel(
    string LangKey,
    Symbol Icon,
    Func<IReadOnlyList<ExhibitModel>, Task<bool>> Handler,
    Func<IReadOnlyList<ExhibitModel>, bool>? CanExecute = null)
{
    public static ExplorerActionModel Noop { get; } = new(string.Empty, Symbol.Box, _ => Task.FromResult(false));
}
