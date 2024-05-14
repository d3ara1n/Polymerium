using Microsoft.Extensions.Logging;
using Polymerium.App.Tasks;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Services
{
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
            instanceManager.InstanceLaunching += InstanceManager_InstanceLaunching;
        }

        private void InstanceManager_InstanceDeploying(InstanceManager sender, InstanceDeployingEventArgs args)
        {
            DeployInstanceTask task = new(args.Handle);
            Enqueue(task);
        }

        private void InstanceManager_InstanceLaunching(InstanceManager sender, InstanceLaunchingEventArgs args)
        {
            LaunchInstanceTask task = new(args.Handle);
            Enqueue(task);
        }

        public void SetHandler(Action<TaskBase> action)
        {
            handler = action;
        }

        public void Enqueue(TaskBase task)
        {
            _logger.LogInformation("Start task {key}({mode})", task.Key, task.GetType().Name);
            _notificationService.PopInformation($"{task.Stage} started");
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
                _notificationService.PopSuccess($"{task.Stage} finished in {time}s");
                task.Unsubscribe(Track);
            }

            if (args.State == TaskState.Faulted)
            {
                var time = (DateTimeOffset.Now - task.CreatedAt).Seconds;
                _logger.LogInformation("Task {model}({task}) faulted in {time}s, {state}", task.GetType().Name,
                    args.Key,
                    time, args.State);
                _notificationService.PopError(
                    $"{task.Stage} failed due to {task.FailureReason?.Message ?? "unknown reason"}");
                task.Unsubscribe(Track);
            }
        }
    }
}