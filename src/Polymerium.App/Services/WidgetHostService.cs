using System;
using System.Collections.Generic;
using Polymerium.App.Widgets;

namespace Polymerium.App.Services;

// 这不是什么插件化的模块，所有小工具都是一次性集中封装的，因此后端共用同一个服务，后缀 Host 表示一对多
public class WidgetHostService(PersistenceService persistenceService)
{
    #region Indicator Constants

    private const string INDICATOR_IS_PINNED = "global.is_pinned";

    #endregion

    #region Private

    private readonly IDictionary<string, WidgetContext> _cachedContexts = new Dictionary<string, WidgetContext>();

    #endregion

    public Type[] WidgetTypes => [typeof(NoteWidget)];

    public WidgetContext GetOrCreateContext(string key, string widgetId)
    {
        if (_cachedContexts.TryGetValue(widgetId, out var context))
        {
            context.Key = key;
            return context;
        }

        context = new(widgetId, this) { Key = key };
        _cachedContexts.Add(widgetId, context);
        return context;
    }

    #region Accessors

    public bool GetIsPinned(string key, string widgetId) =>
        persistenceService.GetWidgetLocalData<bool>(key, widgetId, INDICATOR_IS_PINNED);

    public void SetIsPinned(string key, string widgetId, bool pinned) =>
        persistenceService.SetWidgetLocalData(key, widgetId, INDICATOR_IS_PINNED, pinned);

    public T? GetLocalData<T>(string key, string widgetId, string indicator) =>
        persistenceService.GetWidgetLocalData<T>(key, widgetId, indicator);

    public void SetLocalData<T>(string key, string widgetId, string indicator, T? data) =>
        persistenceService.SetWidgetLocalData(key, widgetId, indicator, data);

    #endregion
}
