using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polymerium.App.Data;
using Polymerium.Core;

namespace Polymerium.App.Services;

public sealed class DataStorage : IDisposable
{
    private readonly IFileBaseService _fileBaseService;

    public DataStorage(IFileBaseService fileBaseService)
    {
        _fileBaseService = fileBaseService;
    }

    public void Dispose()
    {
        // 保存
        GC.SuppressFinalize(this);
    }

    public bool Save<TModel, TData>(TData data)
        where TModel : RefinedModelBase<TData>, new()
    {
        var model = new TModel();
        model.Apply(data);
        var json = JsonConvert.SerializeObject(model, model.SerializerSettings);
        _fileBaseService.WriteAllText(model.Location, json);
        return true;
    }

    public bool SaveList<TModel, TData>(IEnumerable<TData> data)
        where TModel : RefinedModelBase<TData>, new()
    {
        var model = new TModel();
        var models = data.Select(x =>
        {
            var y = new TModel();
            y.Apply(x);
            return y;
        });
        var json = JsonConvert.SerializeObject(models, model.SerializerSettings);
        _fileBaseService.WriteAllText(model.Location, json);
        return true;
    }

    public TData Load<TModel, TData>(Func<TData>? factory = null)
        where TModel : RefinedModelBase<TData>, new()
    {
        var model = new TModel();
        if (_fileBaseService.TryReadAllText(model.Location, out var text))
        {
            JsonConvert.PopulateObject(text, model, model.SerializerSettings);
            return model.Extract();
        }

        return factory!();
    }

    public IEnumerable<TData> LoadList<TModel, TData>(Func<IEnumerable<TData>>? factory = null)
        where TModel : RefinedModelBase<TData>, new()
    {
        var model = new TModel();
        if (_fileBaseService.TryReadAllText(model.Location, out var text))
        {
            var list = JsonConvert.DeserializeObject<IEnumerable<TModel>>(
                text,
                model.SerializerSettings
            );
            return list?.Select(x => x.Extract()) ?? Enumerable.Empty<TData>();
        }

        return factory != null ? factory() : Enumerable.Empty<TData>();
    }
}