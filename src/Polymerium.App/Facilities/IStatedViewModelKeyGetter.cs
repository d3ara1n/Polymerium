using System;

namespace Polymerium.App.Facilities;

public interface IStatedViewModelKeyGetter
{
    public string ViewStateKey { get; }
}
