using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

// NOTE: Info 是否赋值即「能否取到信息」——未赋值（null）统一表达加载失败
//  （Modpack 网络 IO 失败 / Recipe id 无法解析），由组头公共层渲染重试占位。
public abstract class GroupInfoModelBase : ModelBase;
