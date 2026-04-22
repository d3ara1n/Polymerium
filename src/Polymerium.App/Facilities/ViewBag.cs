using System;

namespace Polymerium.App.Facilities;

public class ViewBag
{
    private readonly ViewBagFactory? _factory;

    public ViewBag(ViewBagFactory factory) => _factory = factory;

    public ViewBag(object? parameter) => Parameter = parameter;

    public object? Parameter => field ?? _factory?.Bag;

    public bool IsEmpty => Parameter is null;

    public T? GetParameter<T>() where T : notnull
    {
        if (Parameter is T t)
        {
            return t;
        }

        return default;
    }

    public T GetRequiredParameter<T>() where T : notnull
    {
        if (Parameter is T t)
        {
            return t;
        }

        throw new ArgumentOutOfRangeException(nameof(Parameter));
    }
}
