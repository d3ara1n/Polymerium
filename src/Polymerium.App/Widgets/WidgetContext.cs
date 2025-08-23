using Polymerium.App.Services;

namespace Polymerium.App.Widgets
{
    public class WidgetContext(string id, WidgetHostService service)
    {
        public required string Key { get; set; }
        public string Id => id;

        // 代理到 WidgetHostService 中
        public bool IsPinned
        {
            get => service.GetIsPinned(Key, Id);
            set => service.SetIsPinned(Key, Id, value);
        }

        public T? GetLocalData<T>(string indicator) => service.GetLocalData<T>(Key, Id, indicator);

        public void SetLocalData<T>(string indicator, T? data) => service.SetLocalData(Key, Id, indicator, data);

        // 对于 T Data { get; set; } 该在什么时候保存到数据库，是 Data 本身是个 record 只有覆盖整个对象时写入数据库还是 Context 对象在 Dispose 时写入
        // 抑或是精细化的 T Data 拆分到多个数据库条目并形成键值对 Key-WidgetId-DataId-Data 作为一条目的方式
        // 后者通过 Set<T>(Get<T>() with { Value = newValue }) 的方式实现读写
    }
}
