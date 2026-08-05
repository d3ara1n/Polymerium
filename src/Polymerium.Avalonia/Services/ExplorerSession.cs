using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Models;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Pref;

namespace Polymerium.Avalonia.Services;

// NOTE: explorer 只通过这个宿主中性的契约与 session 对话。任何 Trident 持久化类型（如
//  Profile.Rice.Entry）都不会出现在这里——它是某个 session 实现的私有货物，对兄弟 session 不可见。
//  session 只定义动作，由实现决定怎么做（开哪种弹窗、怎么落盘、携带什么数据）。
public abstract class ExplorerSession
{
    public abstract string Title { get; }

    public abstract Bitmap? Background { get; }

    // NOTE: null 表示宿主没有版本语境（如 recipe），加载器/版本过滤开关应隐藏；非 null 即过滤基准
    public abstract Filter? InitialFilter { get; }

    public abstract ExhibitModel BuildExhibit(Exhibit hit);

    // NOTE: 把一个 exhibit 的 State 还原到提交基线，从 session 自带的数据现算（无需在模型上缓存基线字段）
    public abstract void RevertState(ExhibitModel exhibit);

    // NOTE: 打开宿主专属的详情弹窗。modifyPending 与 findExisting 是 explorer 传入的两个回调：前者
    //  回报待定区状态变更，后者复用已存在的模型实例（依赖项同时也是搜索结果/待定项时保持状态同步）。
    //  其余一切（弹窗类型、ViewPackage 递归、LinkExhibit、Undo 组合）都是 session 自己的事。
    public abstract Task ViewExhibitAsync(ExhibitModel exhibit,
                                          Action<ExhibitModel> modifyPending,
                                          Func<ProjectIdentifier, ExhibitModel?> findExisting);

    // NOTE: 落盘待定改动。instance 原地修改随身携带的 Entry（零 lookup），recipe 更新自己的配方存储。
    //  返回 false 表示写入失败，待定区应保留等待重试。
    public abstract Task<bool> CollectAsync(IReadOnlyList<ExhibitModel> pending);

    public virtual void Validate() { }
}
