using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Services
{
    public class LocalizationService
    {
        private readonly ResourceManager _resourceManager;
        private readonly ResourceContext _resourceContext;

        public LocalizationService(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _resourceContext = resourceManager.CreateResourceContext();
        }
    }
}
