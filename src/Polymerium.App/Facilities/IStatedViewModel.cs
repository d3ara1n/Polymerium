using System;

namespace Polymerium.App.Facilities;

public interface IStatedViewModel<T>
    where T : class
{
    T? ViewState { get; set; }
}
