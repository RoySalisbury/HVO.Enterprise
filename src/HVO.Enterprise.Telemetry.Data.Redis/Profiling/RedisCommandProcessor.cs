using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Data.Common;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace HVO.Enterprise.Telemetry.Data.Redis.Profiling
{
    /// <summary>
    /// Processes profiled Redis commands and creates <see cref="Activity"/> instances
    /// with OpenTelemetry semantic conventions.
    /// </summary>
    public sealed class RedisCommandProcessor
    {
        private readonly RedisTelemetryOptions _options;
        private readonly ActivitySource _activitySource;

        /// <summary>
        /// Initializes a new instance of <see cref="RedisCommandProcessor"/>.
        /// </summary>
        /// <param name="options">Telemetry options.</param>
        public RedisCommandProcessor(RedisTelemetryOptions? options = null)
        {
            _options = options ?? new RedisTelemetryOptions();
            _activitySource = RedisActivitySource.Source;
        }

        /// <summary>
        /// Processes a collection of profiled Redis commands, creating Activities for each.
        /// </summary>
        /// <param name="commands">The profiled commands from a finished session.</param>
        public void ProcessCommands(ProfiledCommandEnumerable commands)
        {
            foreach (var command in commands)
            {
                ProcessCommand(command);
            }
        }

        /// <summary>
        /// Processes a single profiled Redis command.
        /// </summary>
        /// <param name="command">The profiled command.</param>
        public void ProcessCommand(IProfiledCommand command)
        {
            if (command == null)
                return;

            var commandName = command.Command ?? "UNKNOWN";
            var activity = _activitySource.StartActivity($"redis.{commandName}", ActivityKind.Client);

            if (activity == null)
                return;

            try
            {
                activity.SetTag(DataActivityTags.DbSystem, DataActivityTags.SystemRedis);

                if (_options.RecordCommands)
                {
                    activity.SetTag(DataActivityTags.DbOperation, commandName);
                    activity.SetTag(DataActivityTags.DbStatement, commandName);
                }

                if (_options.RecordDatabaseIndex)
                {
                    activity.SetTag(DataActivityTags.DbRedisDatabaseIndex, command.Db);
                }

                if (_options.RecordEndpoint && command.EndPoint != null)
                {
                    var endpoint = command.EndPoint.ToString();
                    if (endpoint != null)
                    {
                        var parts = endpoint.Split(':');
                        if (parts.Length >= 1)
                            activity.SetTag(DataActivityTags.ServerAddress, parts[0]);
                        if (parts.Length >= 2 && int.TryParse(parts[1], out var port))
                            activity.SetTag(DataActivityTags.ServerPort, port);
                    }
                }

                activity.SetTag(DataActivityTags.DbOperationDuration, command.ElapsedTime.TotalMilliseconds);

                activity.SetStatus(ActivityStatusCode.Ok);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
