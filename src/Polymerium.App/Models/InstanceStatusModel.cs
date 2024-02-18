using Polymerium.App.Extensions;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using System;

namespace Polymerium.App.Models;

// 用于从 InstanceManager 中跟踪状态并反馈到界面上
public record InstanceStatusModel
{
    public static readonly InstanceStatusModel DUMMY = new(ProfileManager.DUMMY_KEY);
    private uint count;
    private bool endless = true;
    private DeployStage stage = DeployStage.CheckArtifact;
    private string stageText = string.Empty;
    private InstanceState state = InstanceState.Idle;
    private uint totalCount = 1;

    public InstanceStatusModel(string key)
    {
        Key = key;

        State = this.ToBindable(x => x.state, (x, v) => x.state = v);
        Stage = this.ToBindable(x => x.stage, (x, v) => x.stage = v);
        StageText = this.ToBindable(x => x.stageText, (x, v) => x.stageText = v);
        Endless = this.ToBindable(x => x.endless, (x, v) => x.endless = v);
        Count = this.ToBindable(x => x.count, (x, v) => x.count = v);
        TotalCount = this.ToBindable(x => x.totalCount, (x, v) => x.totalCount = v);
    }

    public string Key { get; }

    public Bindable<InstanceStatusModel, InstanceState> State { get; }
    public Bindable<InstanceStatusModel, string> StageText { get; }
    public Bindable<InstanceStatusModel, DeployStage> Stage { get; }
    public Bindable<InstanceStatusModel, bool> Endless { get; }
    public Bindable<InstanceStatusModel, uint> Count { get; }
    public Bindable<InstanceStatusModel, uint> TotalCount { get; }

    public void OnStateChanged(InstanceState changed)
    {
        State.Value = changed;
    }

    public void OnStageChanged(DeployStage changed)
    {
        Stage.Value = changed;
        StageText.Value = changed switch
        {
            DeployStage.CheckArtifact => "Checking artifact...",
            DeployStage.InstallVanilla => "Installing vanilla...",
            DeployStage.ResolveAttachments => "Resolving attachments...",
            DeployStage.ProcessLoaders => "Processing loaders...",
            DeployStage.BuildArtifact => "Building artifact...",
            DeployStage.BuildTransient => "Building transient...",
            DeployStage.SolidifyTransient => "Solidifying transient...",
            _ => throw new NotImplementedException()
        };
        if (changed != DeployStage.SolidifyTransient)
        {
            Endless.Value = true;
            Count.Value = 0;
            TotalCount.Value = 1;
        }
    }

    public void OnProgressChanged(uint value, uint total)
    {
        Endless.Value = false;
        Count.Value = value;
        TotalCount.Value = total;
    }
}