using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Context.Providers;

namespace HVO.Enterprise.Telemetry.Logging.Enrichers
{
    /// <summary>
    /// Enriches log entries with user authentication context (user ID, username).
    /// </summary>
    /// <remarks>
    /// Delegates to the existing <see cref="IUserContextAccessor"/> infrastructure.
    /// Sensitive fields (email, tenant ID) are not included by default to avoid
    /// PII leakage in logs. Override behavior by implementing a custom
    /// <see cref="ILogEnricher"/>.
    /// </remarks>
    public sealed class UserContextLogEnricher : ILogEnricher
    {
        private readonly IUserContextAccessor _userAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextLogEnricher"/> class.
        /// </summary>
        /// <param name="userAccessor">
        /// Optional user context accessor. If <c>null</c>, uses
        /// <see cref="DefaultUserContextAccessor"/> which returns <c>null</c>
        /// unless a platform-specific accessor is registered.
        /// </param>
        public UserContextLogEnricher(IUserContextAccessor? userAccessor = null)
        {
            _userAccessor = userAccessor ?? new DefaultUserContextAccessor();
        }

        /// <inheritdoc />
        public void Enrich(IDictionary<string, object?> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var userContext = _userAccessor.GetUserContext();
            if (userContext == null)
                return;

            if (!string.IsNullOrEmpty(userContext.UserId))
                properties["UserId"] = userContext.UserId;

            if (!string.IsNullOrEmpty(userContext.Username))
                properties["Username"] = userContext.Username;
        }
    }
}
