using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Snapshots;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.Services;

public class GarbageCollector(
    IFreeSql freeSql,
    ISnapshotStoreFactory snapshotStoreFactory,
    ProfileManager profileManager,
    ILogger<GarbageCollector> logger)
{
    public void Execute(IProgress<double?> progress)
    {
        progress.Report(null);

        var activeKeys = profileManager.Profiles.Select(x => x.Item1).ToHashSet();
        var activeUuids = freeSql.Select<PersistenceService.Account>().ToList(x => x.Uuid).ToHashSet();
        var activeKeyList = activeKeys.ToList();

        var totalSteps = 8 + activeKeyList.Count;
        var completed = 0;

        var orphanSelectorKeys = freeSql
                                .Select<PersistenceService.AccountSelector>()
                                .ToList(x => x.Key)
                                .Where(k => !activeKeys.Contains(k))
                                .ToList();
        if (orphanSelectorKeys.Count > 0)
        {
            freeSql
               .Delete<PersistenceService.AccountSelector>()
               .Where(x => orphanSelectorKeys.Contains(x.Key))
               .ExecuteAffrows();
        }

        var danglingSelectorUuids = freeSql
                                   .Select<PersistenceService.AccountSelector>()
                                   .ToList(x => x.Uuid)
                                   .Where(u => !activeUuids.Contains(u))
                                   .ToList();
        if (danglingSelectorUuids.Count > 0)
        {
            freeSql
               .Delete<PersistenceService.AccountSelector>()
               .Where(x => danglingSelectorUuids.Contains(x.Uuid))
               .ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        var orphanActionKeys = freeSql
                              .Select<PersistenceService.Action>()
                              .ToList(x => x.Key)
                              .Distinct()
                              .Where(k => !activeKeys.Contains(k))
                              .ToList();
        if (orphanActionKeys.Count > 0)
        {
            freeSql.Delete<PersistenceService.Action>().Where(x => orphanActionKeys.Contains(x.Key)).ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        var orphanActivityKeys = freeSql
                                .Select<PersistenceService.Activity>()
                                .ToList(x => x.Key)
                                .Distinct()
                                .Where(k => !activeKeys.Contains(k))
                                .ToList();
        if (orphanActivityKeys.Count > 0)
        {
            freeSql
               .Delete<PersistenceService.Activity>()
               .Where(x => orphanActivityKeys.Contains(x.Key))
               .ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        var orphanViewStateKeys = freeSql
                                 .Select<PersistenceService.ViewState>()
                                 .ToList(x => x.Key)
                                 .Where(k => !activeKeys.Contains(k))
                                 .ToList();
        if (orphanViewStateKeys.Count > 0)
        {
            freeSql
               .Delete<PersistenceService.ViewState>()
               .Where(x => orphanViewStateKeys.Contains(x.Key))
               .ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        var orphanWidgetKeys = freeSql
                              .Select<PersistenceService.WidgetLocalSection>()
                              .ToList(x => x.Key)
                              .Distinct()
                              .Where(k => !activeKeys.Contains(k))
                              .ToList();
        if (orphanWidgetKeys.Count > 0)
        {
            freeSql
               .Delete<PersistenceService.WidgetLocalSection>()
               .Where(x => orphanWidgetKeys.Contains(x.Key))
               .ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        var orphanPinnedKeys = freeSql
                              .Select<PersistenceService.PinnedInstance>()
                              .ToList(x => x.Key)
                              .Where(k => !activeKeys.Contains(k))
                              .ToList();
        if (orphanPinnedKeys.Count > 0)
        {
            freeSql
               .Delete<PersistenceService.PinnedInstance>()
               .Where(x => orphanPinnedKeys.Contains(x.Key))
               .ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        var orphanTagKeys = freeSql
                           .Select<PersistenceService.InstanceTag>()
                           .ToList(x => x.Key)
                           .Distinct()
                           .Where(k => !activeKeys.Contains(k))
                           .ToList();
        if (orphanTagKeys.Count > 0)
        {
            freeSql.Delete<PersistenceService.InstanceTag>().Where(x => orphanTagKeys.Contains(x.Key)).ExecuteAffrows();
        }

        progress.Report((double)++completed / totalSteps);

        foreach (var key in activeKeyList)
        {
            var snapshotDir = PathDef.Default.DirectoryOfSnapshots(key);
            if (!Directory.Exists(snapshotDir))
            {
                progress.Report((double)++completed / totalSteps);
                continue;
            }

            var store = snapshotStoreFactory.Open(key);
            try
            {
                store.DeleteOrphanReferences();
                var referencedHashes = store.GetAllReferencedHashes();
                var objectsDir = PathDef.Default.DirectoryOfSnapshotObjects(key);
                if (Directory.Exists(objectsDir))
                {
                    foreach (var prefixDir in Directory.GetDirectories(objectsDir))
                    {
                        foreach (var file in Directory.GetFiles(prefixDir))
                        {
                            var hash = Path.GetFileName(file);
                            if (!referencedHashes.Contains(hash))
                            {
                                File.Delete(file);
                            }
                        }
                    }
                }
            }
            finally
            {
                store.Dispose();
            }

            progress.Report((double)++completed / totalSteps);
        }

        logger.LogDebug("GC completed");
    }
}
