using Microsoft.Extensions.Logging;
using Polymerium.App.Tasks;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using System;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Services;

public class TaskService
{
    private readonly ILogger<TaskService> _logger;
    private readonly NotificationService _notificationService;

    private Action<TaskBase>? handler;

    public TaskService(ILogger<TaskService> logger, InstanceManager instanceManager,
        NotificationService notificationService)
    {
        _notificationService = notificationService;
        _logger = logger;

        instanceManager.InstanceDeploying += InstanceManager_InstanceDeploying;
    }

    private void InstanceManager_InstanceDeploying(InstanceManager sender, InstanceDeployingEventArgs args)
    {
        var task = new DeployInstanceTask(args.Handle);
        Enqueue(task);
    }

    public void SetHandler(Action<TaskBase> action)
    {
        handler = action;
    }

    private void Enqueue(TaskBase task)
    {
        _logger.LogInformation("Start task {key}({mode})", task.Key, task.GetType().Name);
        task.Subscribe(Track);
        handler?.Invoke(task);
    }

    private void Track(TaskBase task, TaskProgressUpdatedEventArgs args)
    {
        if (args.State == TaskState.Finished)
        {
            var time = (DateTimeOffset.Now - task.CreatedAt).Seconds;
            _logger.LogInformation("Task {model}({task}) ended in {time}s, {state}", task.GetType().Name, args.Key,
                time, args.State);
            _notificationService.PopInformation($"Task {task.Stage} finished in {time}s");
            task.Unsubscribe(Track);
        }

        if (args.State == TaskState.Faulted)
        {
            var time = (DateTimeOffset.Now - task.CreatedAt).Seconds;
            _logger.LogInformation("Task {model}({task}) faulted in {time}s, {state}", task.GetType().Name, args.Key,
                time, args.State);
            _notificationService.PopError(
                $"{task.Stage} failed due to {task.FailureReason?.Message ?? "unknown reason"}");
            task.Unsubscribe(Track);
        }
    }
}