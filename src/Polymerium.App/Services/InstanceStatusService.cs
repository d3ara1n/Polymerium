using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Services
{
    // 具有状态的 Instance 被集中作为列表管理，仅用于 UI 层展示，只需要 Profile 就用 ProfileManager
    public class InstanceStatusService
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly ObservableCollection<InstanceStatusModel> instances;

        public InstanceStatusService(ProfileManager profileManger, InstanceManager instanceManager)
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
            instances = new ObservableCollection<InstanceStatusModel>(
                profileManger.Managed.Keys.Select(x => new InstanceStatusModel(x)));

            profileManger.ProfileCollectionChanged += ProfileMangerOnProfileCollectionChanged;
            instanceManager.InstanceDeploying += InstanceManagerOnInstanceDeploying;
            instanceManager.InstanceLaunching += InstanceManagerOnInstanceLaunching;
        }

        private void InstanceManagerOnInstanceLaunching(InstanceManager sender, InstanceLaunchingEventArgs args)
        {
            if (TryFind(args.Key, out InstanceStatusModel? instance))
            {
                _dispatcher.TryEnqueue(() => instance.OnStateChanged(InstanceState.Running));
                args.Handle.StateUpdated += (_, state) =>
                    _dispatcher.TryEnqueue(() =>
                    {
                        if (state == TaskState.Running)
                        {
                            instance.OnStateChanged(InstanceState.Running);
                        }
                        else if (state == TaskState.Idle)
                        {
                            instance.OnStateChanged(InstanceState.Idle);
                        }
                        else if (state == TaskState.Finished)
                        {
                            instance.OnStateChanged(InstanceState.Idle);
                        }
                        else
                        {
                            instance.OnStateChanged(InstanceState.Stopped);
                        }
                    });
            }
        }

        private void InstanceManagerOnInstanceDeploying(InstanceManager sender, InstanceDeployingEventArgs args)
        {
            if (TryFind(args.Key, out InstanceStatusModel? instance))
            {
                _dispatcher.TryEnqueue(() => instance.OnStateChanged(InstanceState.Running));
                args.Handle.FileSolidified += (_, count, total) =>
                {
                    uint original = instance.Count.Value;
                    uint computed = count == total ? 100 : 100 * count / total;
                    if (original != computed)
                    {
                        _dispatcher.TryEnqueue(() => instance.OnProgressChanged(computed, 100));
                    }
                };
                args.Handle.StageUpdated += (_, stage) => _dispatcher.TryEnqueue(() =>
                    instance.OnStageChanged(stage));
                args.Handle.StateUpdated += (_, state) =>
                    _dispatcher.TryEnqueue(() =>
                    {
                        if (state == TaskState.Running)
                        {
                            instance.OnStateChanged(InstanceState.Deploying);
                        }
                        else if (state == TaskState.Idle)
                        {
                            instance.OnStateChanged(InstanceState.Idle);
                        }
                        else if (state == TaskState.Finished)
                        {
                            instance.OnStateChanged(InstanceState.Idle);
                        }
                        else
                        {
                            instance.OnStateChanged(InstanceState.Stopped);
                        }
                    });
            }
        }

        private void ProfileMangerOnProfileCollectionChanged(ProfileManager sender,
            ProfileCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case ProfileCollectionChangedAction.Add:
                    instances.Add(new InstanceStatusModel(args.Key));
                    break;
                case ProfileCollectionChangedAction.Remove:
                    if (TryFind(args.Key, out InstanceStatusModel? instance))
                    {
                        instances.Remove(instance);
                    }

                    break;
            }
        }


        private bool TryFind(string key, [MaybeNullWhen(false)] out InstanceStatusModel result)
        {
            InstanceStatusModel? instance = instances.FirstOrDefault(x => x.Key == key);
            if (instance != null)
            {
                result = instance;
                return true;
            }

            result = null;
            return false;
        }

        public InstanceStatusModel MustHave(string key)
        {
            if (TryFind(key, out InstanceStatusModel? instance))
            {
                return instance;
            }

            return new InstanceStatusModel(key);
        }
    }
}