using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Tasks;

public class InstallModpackTask(string key): TaskBase(key,$"Install {key}", "Preparing...")
{
    public void OnDownload()
    {
        UpdateProgress(TaskState.Running,status: "Downloading pack file...");
    }

    public void OnExtract()
    {
        ReportProgress(status: "Extracting metadata...");
    }

    public void OnExport()
    {
        ReportProgress(status: "Exporting data & files...");
    }

    public void OnError(Exception e)
    {
        UpdateProgress(TaskState.Faulted, failure:e);
    }

    public void OnFinish()
    {
        UpdateProgress(TaskState.Finished, status: "Process finished.");
    }
}
