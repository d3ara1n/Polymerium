using System;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.App.Services;

namespace Polymerium.App.Facilities;

public class SimpleViewStatePersistence(PersistenceService persistenceService)
    : IViewStatePersistence
{
    public void Save(string key, Type stateType, object value) =>
        persistenceService.SetViewState(key, value);

    public object? Load(string key, Type stateType) =>
        persistenceService.GetViewState(key, stateType);
}
