using System;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.Widgets;

public class WidgetContext(string id, IServiceProvider provider, WidgetHostService service)
{
    public required string Key { get; set; }
    public string Id => id;
    public IServiceProvider Provider => provider;

    public bool IsPinned
    {
        get => service.GetIsPinned(Key, Id);
        set => service.SetIsPinned(Key, Id, value);
    }

    public T? GetLocalData<T>(string indicator) => service.GetLocalData<T>(Key, Id, indicator);

    public void SetLocalData<T>(string indicator, T? data) => service.SetLocalData(Key, Id, indicator, data);

    // NOTE: 本地数据按 Key-WidgetId-DataId-Data 键值对落库，Set<T>/Get<T> 整对象读写，
    //  不在 Context 生命周期内隐式保存。
}
