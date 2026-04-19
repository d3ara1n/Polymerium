using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Polymerium.App.Facilities;

namespace Polymerium.App.Services;

public class ViewStateService(PersistenceService persistenceService) : IDisposable
{
    private IDictionary<string, ViewData> _cache = new Dictionary<string, ViewData>();

    public object RetrieveForView(ViewModelBase owner, Type type)
    {
        string? customKey = null;
        if (owner is IStatedViewModelKeyGetter getter)
        {
            customKey = getter.ViewStateKey;
        }
        var key = Id(owner.GetType(), customKey);
        if (_cache.TryGetValue(key, out var data))
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
            var got = persistenceService.GetViewState(key, type);
            var val = got is not null ? got : Activator.CreateInstance(type)!;
            _cache.Add(key, new ViewData(type, val));
            return val;
        }
    }

    public void ReleaseForView(ViewModelBase owner)
    {
        string? customKey = null;
        if (owner is IStatedViewModelKeyGetter getter)
        {
            customKey = getter.ViewStateKey;
        }
        var key = Id(owner.GetType(), customKey);
        if (_cache.TryGetValue(key, out var data))
        {
            data.@Ref--;
            if (data.@Ref <= 0)
            {
                persistenceService.SetViewState(key, data.Value);
                _cache.Remove(key);
            }
        }
    }

    private static string Id(Type type, string? custom = null)
    {
        var assemblyName = type.Assembly.GetName().Name!;
        var fullName = type.FullName ?? type.Name;
        return custom is not null
            ? $"{assemblyName}|{fullName}|{custom}"
            : $"{assemblyName}|{fullName}";
    }

    public void Dispose()
    {
        if (_cache.Count > 0)
        {
            foreach (var (key, data) in _cache)
            {
                persistenceService.SetViewState(key, data.Value);
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
