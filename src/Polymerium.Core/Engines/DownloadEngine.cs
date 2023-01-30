using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polymerium.Core.Engines.Downloading;

namespace Polymerium.Core.Engines;

public class DownloadEngine
{
    private const uint MAX_WORKER = 8;
    private const int MAX_RETRY = 3;

    private readonly ILogger _logger;

    private readonly BlockingCollection<InternalTask> tasks = new();
    private uint workerNumber;

    public DownloadEngine(ILogger<DownloadEngine> logger)
    {
        _logger = logger;
    }

    public void Enqueue(DownloadTask task)
    {
        var internalTask = new InternalTask();
        internalTask.Inner = task;
        tasks.Add(internalTask);
        _logger.LogInformation("Task of file {} added", Path.GetFileName(task.Destination));
        TrySpawn();
    }

    public void Enqueue(DownloadTaskGroup group)
    {
        var internalGroup = new InternalTaskGroup
        {
            Inner = group
        };
        foreach (var task in group.Tasks)
        {
            var internalTask = new InternalTask
            {
                AssociatedGroup = internalGroup,
                Inner = task
            };
            tasks.Add(internalTask);
        }

        _logger.LogInformation("Task group with {} items added", group.TotalCount);
        TrySpawn();
    }

    private void TrySpawn()
    {
        if (tasks.Count > 0)
        {
            var target = Interlocked.Increment(ref workerNumber);
            if (target > MAX_WORKER)
            {
                // 抢到资源但超额
                Interlocked.Decrement(ref workerNumber);
            }
            else
            {
                var worker = new Thread(Work_Work);
                worker.Name = $"Download Worker {target}";
                worker.Priority = ThreadPriority.BelowNormal;
                _logger.LogDebug("Thread {}({}) spawned", worker.Name, worker.ManagedThreadId);
                worker.Start(worker);
            }
        }
    }

    private void Work_Work(object thread)
    {
        var self = thread as Thread;
        TrySpawn();
        while (tasks.TryTake(out var task))
        {
            if (task.Inner.Token.IsCancellationRequested) continue;
            var client = new HttpClient();
            if (!Directory.Exists(Path.GetDirectoryName(task.Inner.Destination)))
                Directory.CreateDirectory(Path.GetDirectoryName(task.Inner.Destination));
            var file = new FileStream(task.Inner.Destination, FileMode.OpenOrCreate, FileAccess.Write);
            try
            {
                var stream = client.GetStreamAsync(task.Inner.Source, task.Inner.Token).Result;
                stream.CopyTo(file);
                stream.Close();
                FinishTask(task, !task.Inner.Token.IsCancellationRequested);
            }
            catch (TaskCanceledException _)
            {
                continue;
            }
            catch (Exception _)
            {
                if (task.RetryCount++ < MAX_RETRY)
                    tasks.Add(task);
                else
                    FinishTask(task, false);
            }

            file.Flush();
            file.Close();
        }

        _logger.LogDebug("Thread {}({}) died", self.Name, self.ManagedThreadId);
        Interlocked.Decrement(ref workerNumber);
    }

    private void FinishTask(InternalTask task, bool succ)
    {
        task.Inner.CompletedCallback?.Invoke(task.Inner, succ);
        task.Inner.waitHandle.Set();
        task.Inner.IsSuccessful = succ;
        _logger.LogInformation("Task completed with file {}", Path.GetFileName(task.Inner.Destination));
        if (task.AssociatedGroup != null)
        {
            var d = Interlocked.Increment(ref task.AssociatedGroup.Inner.downloadedCount);
            task.AssociatedGroup.Inner.CompletedDelegate?.Invoke(task.AssociatedGroup.Inner, task.Inner, d, succ);
            if (d >= task.AssociatedGroup.Inner.TotalCount)
            {
                task.AssociatedGroup.Inner.waitHandle.Set();
                _logger.LogInformation("Task group completed which has {} files", d);
            }
        }
    }
}