using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.App.Tasks;

namespace Polymerium.App.Services
{
    public class TaskService
    {
        public ObservableCollection<TaskBase> Tasks { get; } = new();
    }
}
