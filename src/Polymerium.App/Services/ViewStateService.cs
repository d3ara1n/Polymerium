using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Polymerium.App.Services;

public class ViewStateService(PersistenceService persistenceService) : IDisposable
{
    private IDictionary<Type, ViewData> _cache = new Dictionary<Type, ViewData>();

    public object RetrieveForView(Type owner, Type type)
    {
        if (_cache.TryGetValue(owner, out var data))
        {
            if (data.Type == type)
            {
                data.@Ref++;
                return data.Value;
            }
            else
            {
                // 数据校验失败
                // 不过这个校验只有在单次会话时会进行，Type 本身不会活过单次会话
                // 这也导致只要内存不损坏，typeof(Owner.State)永远等于当初注册的data.Type
                throw new UnreachableException();
            }
        }
        else
        {
            var got = persistenceService.GetViewState(Id(owner), type);
            var val = got is not null ? got : Activator.CreateInstance(type)!;
            _cache.Add(owner, new ViewData(type, val));
            return val;
        }
    }

    public void ReleaseForView(Type owner)
    {
        if (_cache.TryGetValue(owner, out var data))
        {
            data.@Ref--;
            if (data.@Ref <= 0)
            {
                persistenceService.SetViewState(Id(owner), data.Value);
                _cache.Remove(owner);
            }
        }
    }

    private static string Id(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name!;
        var fullName = type.FullName ?? type.Name;
        return $"{assemblyName}|{fullName}";
    }

    public void Dispose()
    {
        if (_cache.Count > 0)
        {
            foreach (var (owner, data) in _cache)
            {
                persistenceService.SetViewState(Id(owner), data.Value);
            }
        }
    }

    #region Nested type: ViewData
    private class ViewData(Type type, object value)
    {
        public Type Type { get; set; } = type;
        public object Value { get; set; } = value;
        public int @Ref { get; set; } = 1;
    }

    #endregion
}
