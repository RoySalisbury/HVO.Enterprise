using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.IIS
{
    /// <summary>
    /// Detects IIS hosting environment across .NET Framework and .NET Core.
    /// Uses a combination of reflection-based System.Web checks, environment variable
    /// inspection, and process name matching to determine if the application is
    /// running under IIS (w3wp.exe).
    /// </summary>
    public static class IisHostingEnvironment
    {
        private static readonly Lazy<bool> _isIisHosted = new Lazy<bool>(DetectIis);

        /// <summary>
        /// Gets whether the application is running under IIS.
        /// The result is cached after the first evaluation.
        /// </summary>
        public static bool IsIisHosted => _isIisHosted.Value;

        /// <summary>
        /// Gets the IIS worker process ID if hosted in IIS; otherwise <c>null</c>.
        /// </summary>
        public static int? WorkerProcessId => IsIisHosted
            ? Process.GetCurrentProcess().Id
            : (int?)null;

        /// <summary>
        /// Gets a human-readable description of the detected hosting environment.
        /// </summary>
        public static string EnvironmentDescription
        {
            get
            {
                if (!IsIisHosted)
                    return "Not running under IIS";

                var processName = GetCurrentProcessName();
                return $"IIS (process: {processName}, PID: {Process.GetCurrentProcess().Id})";
            }
        }

        private static bool DetectIis()
        {
            // 1. .NET Framework detection via System.Web.Hosting.HostingEnvironment.IsHosted
            if (DetectViaHostingEnvironment())
                return true;

            // 2. Check IIS-specific environment variables
            if (DetectViaEnvironmentVariables())
                return true;

            // 3. Check process name as last resort
            return DetectViaProcessName();
        }

        /// <summary>
        /// Detects IIS hosting via reflection on System.Web.Hosting.HostingEnvironment.IsHosted.
        /// This succeeds on .NET Framework 4.8 when running inside IIS.
        /// </summary>
        private static bool DetectViaHostingEnvironment()
        {
            try
            {
                var hostingEnvironmentType = Type.GetType(
                    "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                if (hostingEnvironmentType == null)
                    return false;

                var isHostedProperty = hostingEnvironmentType.GetProperty("IsHosted");
                if (isHostedProperty == null)
                    return false;

                var isHosted = isHostedProperty.GetValue(null);
                return isHosted is bool hosted && hosted;
            }
            catch
            {
                // Not .NET Framework or System.Web not available
                return false;
            }
        }

        /// <summary>
        /// Detects IIS hosting via well-known environment variables set by IIS/ANCM.
        /// </summary>
        private static bool DetectViaEnvironmentVariables()
        {
            // IIS/ANCM sets ASPNETCORE_IIS_HTTPAUTH when running behind IIS
            var iisHttpAuth = Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH");
            if (!string.IsNullOrEmpty(iisHttpAuth))
                return true;

            // Check APP_POOL_ID which is set by IIS
            var appPoolId = Environment.GetEnvironmentVariable("APP_POOL_ID");
            if (!string.IsNullOrEmpty(appPoolId))
                return true;

            return false;
        }

        /// <summary>
        /// Detects IIS hosting by checking if the current process is w3wp.exe.
        /// </summary>
        private static bool DetectViaProcessName()
        {
            var processName = GetCurrentProcessName();
            return string.Equals(processName, "w3wp", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentProcessName()
        {
            try
            {
                return Process.GetCurrentProcess().ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
