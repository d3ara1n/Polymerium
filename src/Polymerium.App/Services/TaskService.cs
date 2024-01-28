using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Services;

public class TaskService(ILogger<TaskService> logger, IServiceProvider provider)
{
    private readonly IList<TaskBase> tasks = new List<TaskBase>();

    private Action<TaskBase>? handler;

    public void SetHandler(Action<TaskBase> action)
    {
        handler = action;
    }

    public T Create<T>(params object[] parameters) where T : TaskBase
    {
        var task = ActivatorUtilities.CreateInstance<T>(provider, parameters);
        return task;
    }

    public void Enqueue(TaskBase task)
    {
        logger.LogInformation("Start task {key}({type})", task.Key, task.GetType().Name);
        tasks.Add(task);
        handler?.Invoke(task);
        task.Start();
    }

    public T? Find<T>(string key) where T : TaskBase
    {
        return (T?)tasks.FirstOrDefault(x => x is T && x.Key == key);
    }
}