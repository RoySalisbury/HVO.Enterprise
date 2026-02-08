namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Cached environment information.
    /// </summary>
    internal sealed class EnvironmentInfo
    {
        public string ApplicationName { get; set; } = string.Empty;

        public string ApplicationVersion { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public string OsType { get; set; } = string.Empty;

        public string OsVersion { get; set; } = string.Empty;

        public string RuntimeName { get; set; } = string.Empty;

        public string RuntimeVersion { get; set; } = string.Empty;

        public string DeploymentEnvironment { get; set; } = string.Empty;

        public int ProcessId { get; set; }

        public int CpuCount { get; set; }
    }
}
