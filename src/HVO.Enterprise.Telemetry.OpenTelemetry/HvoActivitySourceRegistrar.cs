using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Discovers all HVO ActivitySource names and registers them with
    /// the OpenTelemetry TracerProvider.
    /// </summary>
    internal sealed class HvoActivitySourceRegistrar
    {
        private readonly Configuration.TelemetryOptions _telemetryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="HvoActivitySourceRegistrar"/> class.
        /// </summary>
        /// <param name="telemetryOptions">The telemetry options containing activity source names.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetryOptions"/> is null.</exception>
        public HvoActivitySourceRegistrar(IOptions<Configuration.TelemetryOptions> telemetryOptions)
        {
            if (telemetryOptions == null) throw new ArgumentNullException(nameof(telemetryOptions));
            _telemetryOptions = telemetryOptions.Value;
        }

        /// <summary>
        /// Returns all activity source names configured in HVO telemetry.
        /// Includes the default HVO source and any user-configured sources.
        /// </summary>
        /// <returns>An enumerable of activity source names.</returns>
        public IEnumerable<string> GetSourceNames()
        {
            yield return "HVO.Enterprise.Telemetry";
            yield return "HVO.Enterprise.Telemetry.Http";
            yield return "HVO.Enterprise.Telemetry.Database";

            if (_telemetryOptions.ActivitySources != null)
            {
                foreach (var source in _telemetryOptions.ActivitySources)
                {
                    if (source != "HVO.Enterprise.Telemetry"
                        && source != "HVO.Enterprise.Telemetry.Http"
                        && source != "HVO.Enterprise.Telemetry.Database")
                    {
                        yield return source;
                    }
                }
            }
        }
    }
}
