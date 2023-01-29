using System.Diagnostics;
using System.Threading.Tasks;

namespace Polymerium.Core.Stars
{
    public class PlanetBlender
    {
        private readonly PlanetOptions _options;

        public PlanetBlender(PlanetOptions options)
        {
            _options = options;
        }

        public void Start()
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(_options.JavaExecutable, _options.Arguments)
                {
                    WorkingDirectory = _options.WorkingDirectory,
                }
            };
            process.Start();
        }
    }
}