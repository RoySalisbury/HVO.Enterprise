using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Enriches telemetry with environment and runtime context.
    /// </summary>
    public sealed class EnvironmentContextProvider : IContextProvider
    {
        private static readonly Lazy<EnvironmentInfo> CachedEnvironment = new Lazy<EnvironmentInfo>(CaptureEnvironmentInfo);
        private static readonly AsyncLocal<string?> AsyncContextId = new AsyncLocal<string?>();

        /// <inheritdoc />
        public string Name => "Environment";

        /// <inheritdoc />
        public EnrichmentLevel Level => EnrichmentLevel.Minimal;

        /// <inheritdoc />
        public void EnrichActivity(Activity activity, EnrichmentOptions options)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var env = CachedEnvironment.Value;

            activity.SetTag("service.name", env.ApplicationName);
            activity.SetTag("service.version", env.ApplicationVersion);
            activity.SetTag("host.name", env.MachineName);

            if (options.MaxLevel >= EnrichmentLevel.Standard)
            {
                activity.SetTag("os.type", env.OsType);
                activity.SetTag("os.version", env.OsVersion);
                activity.SetTag("runtime.name", env.RuntimeName);
                activity.SetTag("runtime.version", env.RuntimeVersion);
                activity.SetTag("deployment.environment", env.DeploymentEnvironment);

                AddCustomEnvironmentTags(activity, options);
            }

            if (options.MaxLevel >= EnrichmentLevel.Verbose)
            {
                activity.SetTag("process.pid", env.ProcessId);
                activity.SetTag("thread.id", Thread.CurrentThread.ManagedThreadId);
                activity.SetTag("async.context_id", GetAsyncContextId());
                activity.SetTag("host.cpu_count", env.CpuCount);
            }
        }

        /// <inheritdoc />
        public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var env = CachedEnvironment.Value;

            properties["service.name"] = env.ApplicationName;
            properties["service.version"] = env.ApplicationVersion;
            properties["host.name"] = env.MachineName;

            if (options.MaxLevel >= EnrichmentLevel.Standard)
            {
                properties["os.type"] = env.OsType;
                properties["os.version"] = env.OsVersion;
                properties["runtime.name"] = env.RuntimeName;
                properties["runtime.version"] = env.RuntimeVersion;
                properties["deployment.environment"] = env.DeploymentEnvironment;

                AddCustomEnvironmentProperties(properties, options);
            }

            if (options.MaxLevel >= EnrichmentLevel.Verbose)
            {
                properties["process.pid"] = env.ProcessId;
                properties["thread.id"] = Thread.CurrentThread.ManagedThreadId;
                properties["async.context_id"] = GetAsyncContextId();
                properties["host.cpu_count"] = env.CpuCount;
            }
        }

        private static void AddCustomEnvironmentTags(Activity activity, EnrichmentOptions options)
        {
            if (options.CustomEnvironmentTags == null || options.CustomEnvironmentTags.Count == 0)
                return;

            foreach (var entry in options.CustomEnvironmentTags)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                    continue;

                activity.SetTag("env." + entry.Key, entry.Value);
            }
        }

        private static void AddCustomEnvironmentProperties(IDictionary<string, object> properties, EnrichmentOptions options)
        {
            if (options.CustomEnvironmentTags == null || options.CustomEnvironmentTags.Count == 0)
                return;

            foreach (var entry in options.CustomEnvironmentTags)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                    continue;

                properties["env." + entry.Key] = entry.Value;
            }
        }

        private static EnvironmentInfo CaptureEnvironmentInfo()
        {
            var process = Process.GetCurrentProcess();

            return new EnvironmentInfo
            {
                ApplicationName = GetApplicationName(),
                ApplicationVersion = GetApplicationVersion(),
                MachineName = Environment.MachineName,
                OsType = GetOsType(),
                OsVersion = Environment.OSVersion.VersionString,
                RuntimeName = GetRuntimeName(),
                RuntimeVersion = Environment.Version.ToString(),
                DeploymentEnvironment = GetDeploymentEnvironment(),
                ProcessId = process.Id,
                CpuCount = Environment.ProcessorCount
            };
        }

        private static string GetApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
        }

        private static string GetApplicationVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0";
        }

        private static string GetOsType()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return "windows";
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return "linux";
            return "other";
        }

        private static string GetRuntimeName()
        {
#if NET5_0_OR_GREATER
            return ".NET";
#else
            return ".NET Framework";
#endif
        }

        private static string GetDeploymentEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ENVIRONMENT")
                ?? "Production";
        }

        private static string GetAsyncContextId()
        {
            if (AsyncContextId.Value == null)
                AsyncContextId.Value = Guid.NewGuid().ToString("N");

            return AsyncContextId.Value;
        }
    }
}
